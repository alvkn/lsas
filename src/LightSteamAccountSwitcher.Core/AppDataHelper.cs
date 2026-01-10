namespace LightSteamAccountSwitcher.Core;

public static class AppDataHelper
{
    private static string? _appDataPath;

    public static string GetAppDataPath()
    {
        if (_appDataPath != null)
        {
            return _appDataPath;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _appDataPath = Path.Combine(localAppData, "LightSteamAccountSwitcher");

        if (!Directory.Exists(_appDataPath))
        {
            Directory.CreateDirectory(_appDataPath);
        }

        return _appDataPath;
    }

    public static string GetCachePath(string subFolder = "")
    {
        var path = Path.Combine(GetAppDataPath(), "Cache", subFolder);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }
}