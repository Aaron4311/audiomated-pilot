using System.Text.Encodings.Web;
using System.Text.Json;

namespace Audio.Core;

public sealed class ProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _profilesPath;

    public string FilePath => _profilesPath;

    public ProfileService(string? profilesPath = null)
    {
        _profilesPath = profilesPath ?? Path.Combine(AppPaths.DataDirectory, "profiles.json");
    }

    public Dictionary<string, AudioConfig> LoadAll()
    {
        if (!File.Exists(_profilesPath))
            return new Dictionary<string, AudioConfig>(StringComparer.OrdinalIgnoreCase);

        string json = File.ReadAllText(_profilesPath);
        var raw = JsonSerializer.Deserialize<Dictionary<string, AudioConfig>>(json, JsonOptions);
        return new Dictionary<string, AudioConfig>(raw ?? [], StringComparer.OrdinalIgnoreCase);
    }

    public AudioConfig? Get(string name) => LoadAll().TryGetValue(name, out var config) ? config : null;

    public void Save(string name, AudioConfig config)
    {
        var all = LoadAll();
        all[name] = config;
        WriteAll(all);
    }

    public bool Delete(string name)
    {
        var all = LoadAll();
        bool removed = all.Remove(name);
        if (removed)
            WriteAll(all);
        return removed;
    }

    private void WriteAll(Dictionary<string, AudioConfig> all)
    {
        string json = JsonSerializer.Serialize(all, JsonOptions);
        File.WriteAllText(_profilesPath, json);
    }
}
