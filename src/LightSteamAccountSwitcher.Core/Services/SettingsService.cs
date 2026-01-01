using System.Text.Json;

namespace LightSteamAccountSwitcher.Core.Services;

public class AppSettings
{
    public string SteamPath { get; set; } = "";

    public bool AutoClose { get; set; }

    public bool MinimizeToTray { get; set; }
}

public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(AppDataHelper.GetAppDataPath(), "settings.json");

    public static AppSettings Settings { get; private set; } = new();

    public static void Load()
    {
        if (File.Exists(SettingsPath))
        {
            try
            {
                var json = File.ReadAllText(SettingsPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                Settings = new AppSettings();
            }
        }
    }

    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}