using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LightSteamAccountSwitcher.Core;

namespace LightSteamAccountSwitcher.Windows;

public static class IconHelper
{
    public static void CreateIconFromImage(string inputPath, string outputPath)
    {
        try
        {
            using var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];

            // Standard icon size 256x256
            var resized = new TransformedBitmap(frame,
                new ScaleTransform(256.0 / frame.PixelWidth, 256.0 / frame.PixelHeight));

            using var pngStream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(resized));
            encoder.Save(pngStream);
            var pngData = pngStream.ToArray();

            using var icoFile = new FileStream(outputPath, FileMode.Create);
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
            Logger.Error($"Failed to create icon from {inputPath}: {ex.Message}");
        }
    }
}