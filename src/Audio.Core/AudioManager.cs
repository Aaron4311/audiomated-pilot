using System.Runtime.InteropServices;
using System.Threading;
using Audio.Interop;

namespace Audio.Core;

public sealed class AudioManager
{
    private readonly DeviceEnumerator _enumerator = new();
    private readonly ConfigurationService _configService = new();
    private readonly ProfileService _profileService = new();

    public List<AudioDevice> GetPlaybackDevices() => _enumerator.GetActiveDevices(DeviceKind.Playback);

    public List<AudioDevice> GetRecordingDevices() => _enumerator.GetActiveDevices(DeviceKind.Recording);

    public AudioDevice? GetDefaultDevice(DeviceKind kind, DeviceRole role) => _enumerator.GetDefaultDevice(kind, role);

    public void SetPlaybackDevice(string deviceQuery) =>
        SetDefaultEndpoint(deviceQuery, DeviceKind.Playback, ERole.eConsole, ERole.eMultimedia);

    public void SetRecordingDevice(string deviceQuery) =>
        SetDefaultEndpoint(deviceQuery, DeviceKind.Recording, ERole.eConsole, ERole.eMultimedia);

    public void SetCommunicationsDevice(string deviceQuery, DeviceKind? kind = null) =>
        SetDefaultEndpoint(deviceQuery, kind, ERole.eCommunications);

    public void SetAllRoles(string deviceQuery, DeviceKind? kind = null) =>
        SetDefaultEndpoint(deviceQuery, kind, ERole.eConsole, ERole.eMultimedia, ERole.eCommunications);

    private void SetDefaultEndpoint(string deviceQuery, DeviceKind? kind, params ERole[] roles)
    {
        var device = _enumerator.ResolveDevice(deviceQuery, kind);

        using var policyConfig = new PolicyConfigComWrapper();

        foreach (var role in roles)
        {
            int hr = policyConfig.PolicyConfig.SetDefaultEndpoint(device.Id, role);
            if (hr < 0)
                throw new COMException($"SetDefaultEndpoint({role}) failed for '{deviceQuery}'.", hr);
        }
    }

    private AudioConfig CaptureCurrentConfig()
    {
        var playback = GetDefaultDevice(DeviceKind.Playback, DeviceRole.Default);
        var recording = GetDefaultDevice(DeviceKind.Recording, DeviceRole.Default);
        var communications = GetDefaultDevice(DeviceKind.Playback, DeviceRole.Communications);

        return new AudioConfig
        {
            Playback = playback?.FriendlyName,
            Recording = recording?.FriendlyName,
            Communications = communications?.FriendlyName,
        };
    }

    private void ApplyConfigInternal(AudioConfig config)
    {
        if (!string.IsNullOrEmpty(config.Playback))
            SetPlaybackDevice(config.Playback);
        if (!string.IsNullOrEmpty(config.Recording))
            SetRecordingDevice(config.Recording);
        if (!string.IsNullOrEmpty(config.Communications))
            SetCommunicationsDevice(config.Communications, DeviceKind.Playback);
    }

    public void SaveCurrentConfig() => _configService.Save(CaptureCurrentConfig());

    public AudioConfig ApplyConfig()
    {
        var config = _configService.Load()
            ?? throw new InvalidOperationException("No config.json found. Run --save first.");

        ApplyConfigInternal(config);
        return config;
    }

    public void SaveProfile(string profileName) => _profileService.Save(profileName, CaptureCurrentConfig());

    public AudioConfig ApplyProfile(string profileName)
    {
        var config = _profileService.Get(profileName)
            ?? throw new InvalidOperationException($"No profile named '{profileName}' found.");

        ApplyConfigInternal(config);
        return config;
    }

    public bool DeleteProfile(string profileName) => _profileService.Delete(profileName);

    public IReadOnlyDictionary<string, AudioConfig> GetProfiles() => _profileService.LoadAll();

    public string ProfilesFilePath => _profileService.FilePath;

    public void RunWatchMode(Action<string>? log = null, CancellationToken cancellationToken = default)
    {
        if (_configService.Load() is null)
            throw new InvalidOperationException("No config.json found. Run --save first.");

        // Some drivers (notably SteelSeries Sonar/GG) rapidly fire several
        // default-device-changed notifications in a row while switching their
        // own virtual devices around (e.g. during game audio focus changes).
        // Reacting to every single one causes a SetDefaultEndpoint call per
        // event, each of which glitches audio. Coalesce: restart the debounce
        // timer on every new event for the same (flow, role) pair instead of
        // scheduling a new independent one, so only the *settled* state after
        // the burst gets checked/reverted.
        var pending = new Dictionary<(EDataFlow, ERole), CancellationTokenSource>();
        var pendingLock = new object();

        using var watcher = new EndpointWatcher();
        watcher.DefaultDeviceChanged += (flow, role, deviceId) =>
        {
            var key = (flow, role);
            CancellationTokenSource cts;
            lock (pendingLock)
            {
                if (pending.TryGetValue(key, out var existing))
                    existing.Cancel();

                cts = new CancellationTokenSource();
                pending[key] = cts;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(600, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    return; // superseded by a newer event for this flow/role
                }

                lock (pendingLock)
                {
                    if (pending.TryGetValue(key, out var current) && current == cts)
                        pending.Remove(key);
                }

                try
                {
                    // Reload from disk on every check rather than using a
                    // config captured at watch-start: GUI/CLI can rewrite
                    // config.json (e.g. after the user picks a new device)
                    // while this watcher keeps running as a separate
                    // long-lived process.
                    var config = _configService.Load();
                    if (config is not null)
                        RevertIfDrifted(flow, role, deviceId, config, log);
                }
                catch (Exception ex)
                {
                    log?.Invoke($"Watch error: {ex.Message}");
                }
            });
        };

        watcher.Start();
        log?.Invoke("Watching for default device changes...");

        // Blocks until cancelled, or forever with the default token (CLI
        // --watch, stopped externally via Ctrl+C/process kill).
        cancellationToken.WaitHandle.WaitOne();
    }

    private void RevertIfDrifted(EDataFlow flow, ERole role, string? changedDeviceId, AudioConfig config, Action<string>? log)
    {
        string? expectedName = (flow, role) switch
        {
            (EDataFlow.eRender, ERole.eConsole) => config.Playback,
            (EDataFlow.eRender, ERole.eMultimedia) => config.Playback,
            (EDataFlow.eRender, ERole.eCommunications) => config.Communications,
            (EDataFlow.eCapture, ERole.eConsole) => config.Recording,
            (EDataFlow.eCapture, ERole.eMultimedia) => config.Recording,
            _ => null,
        };

        if (string.IsNullOrEmpty(expectedName))
            return;

        var kind = flow == EDataFlow.eRender ? DeviceKind.Playback : DeviceKind.Recording;
        var expectedDevice = _enumerator.ResolveDevice(expectedName, kind);

        if (string.Equals(expectedDevice.Id, changedDeviceId, StringComparison.OrdinalIgnoreCase))
            return;

        log?.Invoke($"Drift detected: {flow}/{role} moved away from '{expectedName}'. Reverting...");

        if (role == ERole.eCommunications)
            SetCommunicationsDevice(expectedName, kind);
        else if (kind == DeviceKind.Playback)
            SetPlaybackDevice(expectedName);
        else
            SetRecordingDevice(expectedName);
    }
}
