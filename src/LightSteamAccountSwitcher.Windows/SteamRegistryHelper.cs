using System.Runtime.Versioning;
using Microsoft.Win32;

namespace LightSteamAccountSwitcher.Windows;

[SupportedOSPlatform("windows")]
public static class SteamRegistryHelper
{
    private const string SteamRegistryPath = @"Software\Valve\Steam";

    public static string? GetSteamPath()
    {
        using var key = Registry.CurrentUser.OpenSubKey(SteamRegistryPath);
        return key?.GetValue("SteamPath") as string;
    }

    public static string? GetAutoLoginUser()
    {
        using var key = Registry.CurrentUser.OpenSubKey(SteamRegistryPath);
        return key?.GetValue("AutoLoginUser") as string;
    }

    public static void SetAutoLoginUser(string username)
    {
        using var key = Registry.CurrentUser.CreateSubKey(SteamRegistryPath);
        key.SetValue("AutoLoginUser", username);
    }

    public static void SetRememberPassword(bool remember)
    {
        using var key = Registry.CurrentUser.CreateSubKey(SteamRegistryPath);
        key.SetValue("RememberPassword", remember ? 1 : 0);
    }
}