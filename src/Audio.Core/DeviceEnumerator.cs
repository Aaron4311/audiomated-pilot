using System.Runtime.InteropServices;
using Audio.Interop;

namespace Audio.Core;

public sealed class DeviceEnumerator
{
    private const uint STGM_READ = 0;

    public List<AudioDevice> GetActiveDevices(DeviceKind kind)
    {
        var dataFlow = kind switch
        {
            DeviceKind.Playback => EDataFlow.eRender,
            DeviceKind.Recording => EDataFlow.eCapture,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        var results = new List<AudioDevice>();

        using var enumeratorWrapper = new MMDeviceEnumeratorComWrapper();
        var enumerator = enumeratorWrapper.Enumerator;

        int hr = enumerator.EnumAudioEndpoints(dataFlow, (uint)DEVICE_STATE.ACTIVE, out var collection);
        ThrowIfFailed(hr, nameof(IMMDeviceEnumerator.EnumAudioEndpoints));

        try
        {
            hr = collection.GetCount(out uint count);
            ThrowIfFailed(hr, nameof(IMMDeviceCollection.GetCount));

            for (uint i = 0; i < count; i++)
            {
                hr = collection.Item(i, out var device);
                ThrowIfFailed(hr, nameof(IMMDeviceCollection.Item));

                try
                {
                    results.Add(ReadDeviceInfo(device, kind));
                }
                finally
                {
                    Marshal.ReleaseComObject(device);
                }
            }
        }
        finally
        {
            Marshal.ReleaseComObject(collection);
        }

        return results;
    }

    private static AudioDevice ReadDeviceInfo(IMMDevice device, DeviceKind kind)
    {
        int hr = device.GetId(out string id);
        ThrowIfFailed(hr, nameof(IMMDevice.GetId));

        hr = device.OpenPropertyStore(STGM_READ, out var store);
        ThrowIfFailed(hr, nameof(IMMDevice.OpenPropertyStore));

        string friendlyName;
        try
        {
            hr = store.GetValue(in PropertyKeys.PKEY_Device_FriendlyName, out var pv);
            try
            {
                friendlyName = hr >= 0 ? (pv.GetStringValue() ?? "(Unknown Device)") : "(Unknown Device)";
            }
            finally
            {
                pv.Dispose();
            }
        }
        finally
        {
            Marshal.ReleaseComObject(store);
        }

        return new AudioDevice(id, friendlyName, kind, IsActive: true);
    }

    public AudioDevice? GetDefaultDevice(DeviceKind kind, DeviceRole role)
    {
        var dataFlow = kind switch
        {
            DeviceKind.Playback => EDataFlow.eRender,
            DeviceKind.Recording => EDataFlow.eCapture,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        var nativeRole = role switch
        {
            DeviceRole.Default => ERole.eConsole,
            DeviceRole.Communications => ERole.eCommunications,
            _ => throw new ArgumentOutOfRangeException(nameof(role)),
        };

        using var enumeratorWrapper = new MMDeviceEnumeratorComWrapper();

        int hr = enumeratorWrapper.Enumerator.GetDefaultAudioEndpoint(dataFlow, nativeRole, out var device);
        if (hr < 0)
            return null;

        try
        {
            return ReadDeviceInfo(device, kind);
        }
        finally
        {
            Marshal.ReleaseComObject(device);
        }
    }

    // Playback and Recording devices are searched independently by design:
    // some virtual audio drivers (e.g. SteelSeries Sonar) expose a render
    // endpoint and a capture endpoint with the exact same friendly name, so a
    // blind cross-collection search can silently resolve to the wrong flow.
    // When kind is null (communications/all-roles verbs, which aren't
    // flow-scoped in the CLI), a name that matches in both collections is
    // ambiguous and must be rejected rather than guessed at.
    public AudioDevice? FindDeviceByExactName(string friendlyName, DeviceKind? kind = null)
    {
        if (kind is not null)
        {
            return GetActiveDevices(kind.Value)
                .FirstOrDefault(d => string.Equals(d.FriendlyName, friendlyName, StringComparison.OrdinalIgnoreCase));
        }

        var playbackMatch = GetActiveDevices(DeviceKind.Playback)
            .FirstOrDefault(d => string.Equals(d.FriendlyName, friendlyName, StringComparison.OrdinalIgnoreCase));
        var recordingMatch = GetActiveDevices(DeviceKind.Recording)
            .FirstOrDefault(d => string.Equals(d.FriendlyName, friendlyName, StringComparison.OrdinalIgnoreCase));

        if (playbackMatch is not null && recordingMatch is not null)
        {
            throw new InvalidOperationException(
                $"'{friendlyName}' matches both a Playback and a Recording device. Use --set-playback or --set-recording to disambiguate.");
        }

        return playbackMatch ?? recordingMatch;
    }

    // Unified device resolution for CLI --set-* arguments: 1-based index,
    // then exact friendly-name match, then substring match, in that order.
    // Throws rather than guessing on any ambiguity (index without a kind
    // hint, or a substring matching more than one device) — MVP-2 proved
    // silent disambiguation can quietly hit the wrong physical device.
    public AudioDevice ResolveDevice(string query, DeviceKind? kind = null)
    {
        if (int.TryParse(query, out int index) && index >= 1)
        {
            if (kind is null)
                throw new InvalidOperationException("Index-based selection requires a playback/recording hint for this command.");

            var list = GetActiveDevices(kind.Value);
            if (index > list.Count)
                throw new InvalidOperationException($"Index {index} out of range for {kind} devices (found {list.Count}).");

            return list[index - 1];
        }

        var exact = FindDeviceByExactName(query, kind);
        if (exact is not null)
            return exact;

        var candidates = (kind is not null
                ? GetActiveDevices(kind.Value)
                : GetActiveDevices(DeviceKind.Playback).Concat(GetActiveDevices(DeviceKind.Recording)))
            .Where(d => d.FriendlyName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return candidates.Count switch
        {
            0 => throw new InvalidOperationException($"No device found matching '{query}'."),
            1 => candidates[0],
            _ => throw new InvalidOperationException(
                $"'{query}' matches {candidates.Count} devices: {string.Join(", ", candidates.Select(d => d.FriendlyName))}. Be more specific."),
        };
    }

    private static void ThrowIfFailed(int hr, string operation)
    {
        if (hr < 0)
            throw new COMException($"{operation} failed.", hr);
    }
}
