using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace LightSteamAccountSwitcher.Windows;

public static class SteamProcessHelper
{
    public static void CloseSteam()
    {
        var processes = Process.GetProcessesByName("steam");
        foreach (var process in processes)
        {
            try
            {
                process.CloseMainWindow();
                if (!process.WaitForExit(5000))
                {
                    process.Kill();
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }

    [SupportedOSPlatform("windows")]
    public static void StartSteam(string args = "")
    {
        var steamPath = SteamRegistryHelper.GetSteamPath();
        if (string.IsNullOrEmpty(steamPath))
        {
            return;
        }

        var exePath = Path.Combine(steamPath, "steam.exe");
        if (File.Exists(exePath))
        {
            Process.Start(new ProcessStartInfo(exePath, args) { UseShellExecute = true });
        }
    }
}