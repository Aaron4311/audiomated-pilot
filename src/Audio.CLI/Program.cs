using Audio.Core;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

var manager = new AudioManager();

try
{
    switch (args[0])
    {
        case "--list":
            RunList(manager);
            return 0;

        case "--set-playback" when args.Length > 1:
            manager.SetPlaybackDevice(args[1]);
            Console.WriteLine($"Playback device set to: {args[1]}");
            return 0;

        case "--set-recording" when args.Length > 1:
            manager.SetRecordingDevice(args[1]);
            Console.WriteLine($"Recording device set to: {args[1]}");
            return 0;

        case "--set-communications" when args.Length > 1:
            manager.SetCommunicationsDevice(args[1], ParseKindHint(args));
            Console.WriteLine($"Communications device set to: {args[1]}");
            return 0;

        case "--set-all" when args.Length > 1:
            manager.SetAllRoles(args[1], ParseKindHint(args));
            Console.WriteLine($"All roles set to: {args[1]}");
            return 0;

        case "--save":
            manager.SaveCurrentConfig();
            Console.WriteLine("Current defaults saved to config.json");
            return 0;

        case "--apply":
        case "--restore":
            var applied = manager.ApplyConfig();
            Console.WriteLine($"Applied from config.json: Playback={applied.Playback}, Recording={applied.Recording}, Communications={applied.Communications}");
            return 0;

        case "--watch":
            manager.RunWatchMode(msg => Console.WriteLine(msg));
            return 0;

        case "--profile" when args.Length > 1:
            var profileApplied = manager.ApplyProfile(args[1]);
            Console.WriteLine($"Applied profile '{args[1]}': Playback={profileApplied.Playback}, Recording={profileApplied.Recording}, Communications={profileApplied.Communications}");
            return 0;

        case "--save-profile" when args.Length > 1:
            manager.SaveProfile(args[1]);
            Console.WriteLine($"Current defaults saved as profile '{args[1]}'");
            return 0;

        case "--delete-profile" when args.Length > 1:
            bool deleted = manager.DeleteProfile(args[1]);
            Console.WriteLine(deleted ? $"Profile '{args[1]}' deleted" : $"No profile named '{args[1]}' found");
            return deleted ? 0 : 1;

        case "--list-profiles":
            var profiles = manager.GetProfiles();
            if (profiles.Count == 0)
            {
                Console.WriteLine("No profiles saved.");
                return 0;
            }
            foreach (var (name, cfg) in profiles)
                Console.WriteLine($"{name}: Playback={cfg.Playback}, Recording={cfg.Recording}, Communications={cfg.Communications}");
            return 0;

        case "--startup":
            int delaySeconds = args.Length > 1 && int.TryParse(args[1], out int overrideSeconds) ? overrideSeconds : 20;
            Console.WriteLine($"Waiting {delaySeconds}s before applying startup configuration...");
            Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
            var startupApplied = manager.ApplyConfig();
            Console.WriteLine($"Startup apply complete: Playback={startupApplied.Playback}, Recording={startupApplied.Recording}, Communications={startupApplied.Communications}");
            return 0;

        default:
            PrintUsage();
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static void RunList(AudioManager manager)
{
    var playback = manager.GetPlaybackDevices();
    var recording = manager.GetRecordingDevices();

    var defaultPlayback = manager.GetDefaultDevice(DeviceKind.Playback, DeviceRole.Default);
    var defaultPlaybackComm = manager.GetDefaultDevice(DeviceKind.Playback, DeviceRole.Communications);
    var defaultRecording = manager.GetDefaultDevice(DeviceKind.Recording, DeviceRole.Default);
    var defaultRecordingComm = manager.GetDefaultDevice(DeviceKind.Recording, DeviceRole.Communications);

    PrintSection("Playback", playback, defaultPlayback?.Id, defaultPlaybackComm?.Id);
    Console.WriteLine();
    PrintSection("Recording", recording, defaultRecording?.Id, defaultRecordingComm?.Id);
}

static void PrintSection(string header, List<AudioDevice> devices, string? defaultId, string? defaultCommId)
{
    Console.WriteLine(header);
    Console.WriteLine();
    for (int i = 0; i < devices.Count; i++)
    {
        var tags = new List<string>();
        if (devices[i].Id == defaultId)
            tags.Add("Default");
        if (devices[i].Id == defaultCommId)
            tags.Add("Default Communication");
        string suffix = tags.Count > 0 ? $" [{string.Join(", ", tags)}]" : "";

        Console.WriteLine($"{i + 1}.");
        Console.WriteLine(devices[i].FriendlyName + suffix);
        if (i < devices.Count - 1)
            Console.WriteLine();
    }
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  Audio.CLI --list");
    Console.WriteLine("  Audio.CLI --set-playback <index|name|partial-name>");
    Console.WriteLine("  Audio.CLI --set-recording <index|name|partial-name>");
    Console.WriteLine("  Audio.CLI --set-communications <index|name|partial-name> [playback|recording]");
    Console.WriteLine("  Audio.CLI --set-all <index|name|partial-name> [playback|recording]");
    Console.WriteLine("  Audio.CLI --save");
    Console.WriteLine("  Audio.CLI --apply | --restore");
    Console.WriteLine("  Audio.CLI --startup [delaySeconds]");
    Console.WriteLine("  Audio.CLI --watch");
    Console.WriteLine("  Audio.CLI --profile <name>");
    Console.WriteLine("  Audio.CLI --save-profile <name>");
    Console.WriteLine("  Audio.CLI --delete-profile <name>");
    Console.WriteLine("  Audio.CLI --list-profiles");
    Console.WriteLine();
    Console.WriteLine("  Index refers to the number shown by --list. Partial name matches");
    Console.WriteLine("  any device whose friendly name contains the given text.");
    Console.WriteLine("  The optional playback/recording hint disambiguates --set-communications");
    Console.WriteLine("  and --set-all (index selection requires it) when a name or index");
    Console.WriteLine("  could refer to either side (e.g. some virtual audio drivers).");
}

static DeviceKind? ParseKindHint(string[] args)
{
    if (args.Length <= 2)
        return null;

    return args[2].ToLowerInvariant() switch
    {
        "playback" => DeviceKind.Playback,
        "recording" => DeviceKind.Recording,
        _ => throw new InvalidOperationException($"Unknown flow hint '{args[2]}', expected 'playback' or 'recording'."),
    };
}
