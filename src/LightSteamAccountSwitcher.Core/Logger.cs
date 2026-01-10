using System.Diagnostics;

namespace LightSteamAccountSwitcher.Core;

public static class Logger
{
    private static readonly string LogPath = Path.Combine(AppDataHelper.GetAppDataPath(), "app.log");
    private static readonly Lock Lock = new();

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Warn(string message)
    {
        Write("WARN", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        var msg = message;
        if (ex != null)
        {
            msg += $"\nException: {ex}";
        }

        Write("ERROR", msg);
    }

    private static void Write(string level, string message)
    {
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

        // IDE Output
        Debug.WriteLine(logEntry);

        // File Output
        try
        {
            lock (Lock)
            {
                File.AppendAllText(LogPath, logEntry + Environment.NewLine);
            }
        }
        catch (Exception)
        {
            // Fallback if file is locked or inaccessible
            Debug.WriteLine($"[Log Failure] Could not write to log file: {message}");
        }
    }
}