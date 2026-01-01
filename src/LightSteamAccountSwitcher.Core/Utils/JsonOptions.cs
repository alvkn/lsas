using System.Text.Json;

namespace LightSteamAccountSwitcher.Core.Utils;

public static class JsonOptions
{
    public static JsonSerializerOptions Default => new() { WriteIndented = false };
}