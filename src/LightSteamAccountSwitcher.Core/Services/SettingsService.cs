using System.Text.Json;
using LightSteamAccountSwitcher.Core.Models;
using LightSteamAccountSwitcher.Core.Utils;

namespace LightSteamAccountSwitcher.Core.Services;

public static class SettingsService
{
    private const int CurrentVersion = 1;
    private static readonly string SettingsPath = Path.Combine(AppDataService.GetAppDataPath(), "settings.json");

    public static AppSettings Settings { get; private set; } = new();

    public static void Load()
    {
        if (!File.Exists(SettingsPath))
        {
            Save();
            return;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            Settings = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppSettings) ?? new AppSettings();

            if (Settings.Version < CurrentVersion)
            {
                Settings.Version = CurrentVersion;
                Save();
            }
        }
        catch
        {
            Settings = new AppSettings();
            Save();
        }
    }

    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, AppJsonContext.Default.AppSettings);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}