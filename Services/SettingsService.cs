using System.Text.Json;
using Glosslate.Models;

namespace Glosslate.Services;

public class SettingsService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _path;

    public SettingsService()
    {
        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsDirectory = Path.Combine(baseDirectory, "Glosslate");
        Directory.CreateDirectory(settingsDirectory);
        _path = Path.Combine(settingsDirectory, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_path))
            return new AppSettings();

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings) =>
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, Options));
}
