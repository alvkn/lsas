using System.Text.Json;
using LightSteamAccountSwitcher.Core.Models;
using LightSteamAccountSwitcher.Core.Utils;

namespace LightSteamAccountSwitcher.Core.Services;

public class VacStatusCache
{
    private const string CacheFileName = "vac_cache.json";

    public List<SteamProfile> LoadCache()
    {
        var path = Path.Combine(AppDataHelper.GetCachePath(), CacheFileName);
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<SteamProfile>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void SaveCache(IEnumerable<SteamProfile> profiles)
    {
        var path = Path.Combine(AppDataHelper.GetCachePath(), CacheFileName);
        try
        {
            var json = JsonSerializer.Serialize(profiles.ToList(), JsonOptions.Default);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save vac cache: {ex.Message}");
        }
    }

    public void UpdateCache(string steamId, bool vac, bool limited)
    {
        var cache = LoadCache();
        var existing = cache.FirstOrDefault(x => x.SteamId64 == steamId);

        if (existing != null)
        {
            existing.VacBanned = vac;
            existing.IsLimitedAccount = limited;
            existing.LastUpdated = DateTime.Now;
        }
        else
        {
            cache.Add(new SteamProfile
            {
                SteamId64 = steamId,
                VacBanned = vac,
                IsLimitedAccount = limited,
                LastUpdated = DateTime.Now
            });
        }

        SaveCache(cache);
    }
}