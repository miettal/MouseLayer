using System.Text.Json;
using System.Text.Json.Serialization;

namespace MouseLayer.Core.Profiles;

public static class ConfigStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static MouseLayerConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            var created = MouseLayerConfig.CreateDefault();
            Save(created, path);
            return created;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<MouseLayerConfig>(json, Options) ?? MouseLayerConfig.CreateDefault();
    }

    public static void Save(MouseLayerConfig config, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, JsonSerializer.Serialize(config, Options));
    }

    public static string GetDefaultConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "MouseLayer", "config.json");
    }
}
