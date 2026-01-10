using System.Text.Json;
using LightSteamAccountSwitcher.Core.Models;
using LightSteamAccountSwitcher.Core.Utils;

namespace LightSteamAccountSwitcher.Core.Services;

public class VacStatusCache
{
    private const string CacheFileName = "vac_cache.json";
    private readonly string _cacheFileName = Path.Combine(AppDataHelper.GetCachePath(), CacheFileName);

    private List<SteamProfile> _cache = [];
    private bool _isLoaded;

    public VacStatusCache()
    {
    }

    private void EnsureLoaded()
    {
        if (_isLoaded) return;
        LoadCache();
        _isLoaded = true;
    }

    public void LoadCache()
    {
        if (!File.Exists(_cacheFileName))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_cacheFileName);
            _cache = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListSteamProfile) ?? [];
        }
        catch
        {
            //
        }
    }

    private void Save(IEnumerable<SteamProfile> profiles)
    {
        try
        {
            var json = JsonSerializer.Serialize(profiles.ToList(), AppJsonContext.Default.ListSteamProfile);
            File.WriteAllText(_cacheFileName, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save vac cache: {ex.Message}");
        }
    }

    public void Update(string steamId, bool vac, bool limited)
    {
        EnsureLoaded();
        var existing = _cache.FirstOrDefault(x => x.SteamId64 == steamId);

        if (existing != null)
        {
            existing.VacBanned = vac;
            existing.IsLimitedAccount = limited;
            existing.LastUpdated = DateTime.Now;
        }
        else
        {
            _cache.Add(new SteamProfile
            {
                SteamId64 = steamId,
                VacBanned = vac,
                IsLimitedAccount = limited,
                LastUpdated = DateTime.Now
            });
        }

        Save(_cache);
    }

    public SteamProfile? Get(string steamId64)
    {
        EnsureLoaded();
        return _cache.FirstOrDefault(x => x.SteamId64 == steamId64);
    }

    public bool IsCacheValid(string steamId)
    {
        EnsureLoaded();
        var profile = _cache.FirstOrDefault(x => x.SteamId64 == steamId);
        return profile != null && DateTime.Now - profile.LastUpdated < TimeSpan.FromDays(1);
    }

    public void ClearCache()
    {
        File.Delete(_cacheFileName);
        _cache.Clear();
        _isLoaded = true;
    }
}