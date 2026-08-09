using System.Text.Encodings.Web;
using System.Text.Json;

namespace Audio.Core;

public sealed class ConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _configPath;

    public ConfigurationService(string? configPath = null)
    {
        _configPath = configPath ?? Path.Combine(AppPaths.DataDirectory, "config.json");
    }

    public AudioConfig? Load()
    {
        if (!File.Exists(_configPath))
            return null;

        string json = File.ReadAllText(_configPath);
        return JsonSerializer.Deserialize<AudioConfig>(json, JsonOptions);
    }

    public void Save(AudioConfig config)
    {
        string json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }
}
