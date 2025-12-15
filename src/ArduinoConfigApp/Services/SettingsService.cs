using System.Text.Json;
using Microsoft.UI.Xaml;

namespace ArduinoConfigApp.Services;

public class SettingsService
{
    private const string SettingsFileName = "appsettings.json";
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArduinoConfigApp",
        SettingsFileName);

    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();

    public AppSettings Settings { get; private set; }

    private SettingsService()
    {
        Settings = LoadSettings();
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // If loading fails, return default settings
        }
        return new AppSettings();
    }

    public void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail if we can't save settings
        }
    }

    public ElementTheme GetCurrentTheme()
    {
        return Settings.Theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    public void SetTheme(string theme)
    {
        Settings.Theme = theme;
        SaveSettings();
    }
}

public class AppSettings
{
    public string Theme { get; set; } = "Default";
    public List<string> RecentConfigurations { get; set; } = new();
    public bool AutoSaveEnabled { get; set; } = false;
    public string? LastOpenedConfiguration { get; set; }
}
