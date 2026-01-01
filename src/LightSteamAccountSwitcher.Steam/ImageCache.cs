using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        var cacheDir = AppDataHelper.GetCachePath("Avatars");
        var path = Path.Combine(cacheDir, $"{steamId}.jpg");
        if (File.Exists(path))
        {
            return path;
        }

        return null;
    }

    public static string? GetCachedIconPath(string steamId)
    {
        var cacheDir = AppDataHelper.GetCachePath("Avatars");
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

        var cacheDir = AppDataHelper.GetCachePath("Avatars");
        var path = Path.Combine(cacheDir, $"{steamId}.jpg");

        try
        {
            var bytes = await _httpClient.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(path, bytes);

            // After download, generate icon for shortcuts
            await Task.Run(() => CreateIcon(steamId, path));

            return path;
        }
        catch (IOException)
        {
            if (File.Exists(path))
            {
                return path;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download avatar for {steamId}: {ex.Message}");
            return null;
        }
    }

    // TODO: move it to separate class and call on shortcut creation, not download
    private static void CreateIcon(string steamId, string jpgPath)
    {
        var iconPath = Path.Combine(AppDataHelper.GetCachePath("Avatars"), $"{steamId}.ico");
        try
        {
            using var stream = new FileStream(jpgPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];

            // Standard icon size 256x256
            var resized = new TransformedBitmap(frame,
                new ScaleTransform(256.0 / frame.PixelWidth, 256.0 / frame.PixelHeight));

            using var pngStream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(resized));
            encoder.Save(pngStream);
            var pngData = pngStream.ToArray();

            using var icoFile = new FileStream(iconPath, FileMode.Create);
            using var writer = new BinaryWriter(icoFile);

            // ICO Header
            writer.Write((short)0); // Reserved
            writer.Write((short)1); // Type (Icon)
            writer.Write((short)1); // Count

            // Directory Entry
            writer.Write((byte)0); // Width (0 = 256)
            writer.Write((byte)0); // Height (0 = 256)
            writer.Write((byte)0); // Color count
            writer.Write((byte)0); // Reserved
            writer.Write((short)1); // Planes
            writer.Write((short)32); // Bit count
            writer.Write(pngData.Length); // Bytes in res
            writer.Write(22); // Image offset (Header 6 + Entry 16)

            // Image Data
            writer.Write(pngData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create icon for {steamId}: {ex.Message}");
        }
    }
}