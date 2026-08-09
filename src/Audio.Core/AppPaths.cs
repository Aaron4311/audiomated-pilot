namespace Audio.Core;

// Shared per-user location for config.json/profiles.json so the CLI, GUI,
// and (later) service all agree on the same files regardless of which
// executable's directory they run from.
internal static class AppPaths
{
    public static string DataDirectory
    {
        get
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AudiomatedPilot");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
