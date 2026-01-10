using System.IO;
using System.Net.Http;
using LightSteamAccountSwitcher.Core;
using LightSteamAccountSwitcher.Core.Services;

namespace LightSteamAccountSwitcher.Steam;

public class ImageCache
{
    private readonly HttpClient _httpClient;

    public ImageCache(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public static string? GetCachedAvatarPath(string steamId)
    {
        var cacheDir = AppDataService.GetCachePath("Avatars");
        var path = Path.Combine(cacheDir, $"{steamId}.jpg");
        if (File.Exists(path))
        {
            return path;
        }

        return null;
    }

    public static string? GetCachedIconPath(string steamId)
    {
        var cacheDir = AppDataService.GetCachePath("Avatars");
        var path = Path.Combine(cacheDir, $"{steamId}.ico");

        return File.Exists(path) ? path : null;
    }

    public static bool IsCacheValid(string steamId)
    {
        var path = GetCachedAvatarPath(steamId);
        if (path == null)
        {
            return false;
        }

        try
        {
            var info = new FileInfo(path);
            return DateTime.Now - info.LastWriteTime < TimeSpan.FromDays(1);
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> DownloadAndCacheAvatar(string steamId, string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        var cacheDir = AppDataService.GetCachePath("Avatars");
        var path = Path.Combine(cacheDir, $"{steamId}.jpg");

        try
        {
            var bytes = await _httpClient.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(path, bytes);

            return path;
        }
        catch (IOException)
        {
            return File.Exists(path) ? path : null;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to download avatar for {steamId}: {ex.Message}");
            return null;
        }
    }

    public void ClearCache()
    {
        var cacheDir = AppDataService.GetCachePath("Avatars");
        if (!Directory.Exists(cacheDir))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(cacheDir))
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // Ignore locked files (currently displayed in UI)
            }
        }
    }
}