using System.IO;
using System.Net.Http;
using System.Xml.Linq;
using LightSteamAccountSwitcher.Core;
using LightSteamAccountSwitcher.Core.Models;
using LightSteamAccountSwitcher.Windows;
using VdfSerializer;
using VdfSerializer.Linq;

namespace LightSteamAccountSwitcher.Steam;

public class SteamService
{
    private readonly HttpClient _httpClient;
    private readonly ImageCache _imageCache;
    private readonly VacStatusCache _vacCache;

    public SteamService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LightSteamAccountSwitcher");

        _imageCache = new ImageCache(_httpClient);
        _vacCache = new VacStatusCache();
    }

    private static string? GetLoginUsersPath()
    {
        var steamPath = SteamRegistryHelper.GetSteamPath();
        return string.IsNullOrEmpty(steamPath) ? null : Path.Combine(steamPath, "config", "loginusers.vdf");
    }

    public static string? GetActiveAccountName()
    {
        return SteamRegistryHelper.GetAutoLoginUser();
    }

    public static string? GetCachedIconPath(string steamId)
    {
        return ImageCache.GetCachedIconPath(steamId);
    }

    public List<SteamAccount> GetSteamUsers()
    {
        var path = GetLoginUsersPath();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return [];
        }

        var accounts = new List<SteamAccount>();
        try
        {
            var vdfText = File.ReadAllText(path);
            var rootNode = VdfConvert.Deserialize(vdfText);

            if (rootNode is not { Key: "users", Value: VObject usersNode })
            {
                return [];
            }

            foreach (var userNode in usersNode)
            {
                var account = CreateSteamAccountFromVdf(userNode);
                if (account != null)
                {
                    accounts.Add(account);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error parsing loginusers.vdf: {ex.Message}");
        }

        return accounts;
    }

    private SteamAccount? CreateSteamAccountFromVdf(KeyValuePair<string, VToken> userNode)
    {
        var steamId64 = userNode.Key;
        if (userNode.Value is not VObject userDetails)
        {
            return null;
        }

        var accountName = userDetails.Value<string>("AccountName")!;
        var personaName = userDetails.Value<string>("PersonaName")!;
        var timestamp = userDetails.Value<string>("Timestamp")!;
        var wantsOffline = userDetails.Value<string>("WantsOfflineMode")!;
        var mostRecent = userDetails.Value<string>("MostRec")!;
        var remember = userDetails.Value<string>("RememberPassword")!;

        if (string.IsNullOrEmpty(accountName))
        {
            return null;
        }

        var ts = long.Parse(timestamp);
        var lastLogin = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;

        // Cache lookup
        var cachedProfile = _vacCache.Get(steamId64);
        var cachedAvatar = ImageCache.GetCachedAvatarPath(steamId64);

        var account = new SteamAccount
        {
            SteamId64 = steamId64,
            AccountName = accountName,
            PersonaName = personaName,
            LastLogin = lastLogin,
            WantsOfflineMode = wantsOffline == "1",
            MostRecent = mostRecent == "1",
            RememberPassword = remember == "1",
            AvatarUrl = cachedAvatar ??
                "https://avatars.steamstatic.com/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb_full.jpg"
        };

        if (cachedProfile == null)
        {
            return account;
        }

        account.IsVacBanned = cachedProfile.VacBanned;
        account.IsLimited = cachedProfile.IsLimitedAccount;

        return account;
    }

    public async Task EnrichAccountInfo(SteamAccount? account)
    {
        if (account == null || string.IsNullOrEmpty(account.SteamId64))
        {
            return;
        }

        var vacCacheValid = _vacCache.IsCacheValid(account.SteamId64);
        var imageCacheValid = ImageCache.IsCacheValid(account.SteamId64);

        if (vacCacheValid && imageCacheValid)
        {
            account.IsProfileLoaded = true;
            return;
        }

        try
        {
            var xmlUrl = $"https://steamcommunity.com/profiles/{account.SteamId64}?xml=1";
            var xmlContent = await _httpClient.GetStringAsync(xmlUrl);

            var doc = XDocument.Parse(xmlContent);
            var root = doc.Root;
            if (root == null)
            {
                return;
            }

            var avatarFull = root.Element("avatarFull")?.Value;
            var vacBanned = root.Element("vacBanned")?.Value;
            var isLimited = root.Element("isLimitedAccount")?.Value;

            // Update Image Cache if needed
            if (!string.IsNullOrEmpty(avatarFull) && !imageCacheValid)
            {
                var localPath = await _imageCache.DownloadAndCacheAvatar(account.SteamId64, avatarFull);
                account.AvatarUrl = localPath ?? avatarFull;
            }

            var isVac = vacBanned == "1";
            var isLimit = isLimited == "1";

            // Update VAC Cache (this also sets LastUpdated)
            _vacCache.Update(account.SteamId64, isVac, isLimit);

            account.IsVacBanned = isVac;
            account.IsLimited = isLimit;
            account.IsProfileLoaded = true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error fetching profile for {account.AccountName}: {ex.Message}");
        }
    }

    public void SwitchAccount(SteamAccount? account, int personaState = 1)
    {
        if (account == null)
        {
            return;
        }

        SteamProcessHelper.CloseSteam();
        PatchLoginUsers(account.SteamId64, personaState);

        if (personaState != -1)
        {
            SetPersonaState(account.SteamId64, personaState);
        }

        SteamRegistryHelper.SetAutoLoginUser(account.AccountName);
        SteamRegistryHelper.SetRememberPassword(true);
        SteamProcessHelper.StartSteam();
    }

    public static void StartSteamLogin()
    {
        SteamProcessHelper.CloseSteam();
        SteamRegistryHelper.SetAutoLoginUser(""); // Clear auto login
        SteamRegistryHelper.SetRememberPassword(false);
        SteamProcessHelper.StartSteam();
    }

    public static void ForgetAccount(string steamId)
    {
        var path = GetLoginUsersPath();
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            var vdfText = File.ReadAllText(path);
            var root = VdfConvert.Deserialize(vdfText);

            if (root.Value is VObject usersObj && usersObj.Remove(steamId))
            {
                File.WriteAllText(path, root.ToString());
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error forgetting account: {ex}");
        }
    }

    private void PatchLoginUsers(string targetSteamId, int personaState = -1)
    {
        var path = GetLoginUsersPath();
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            var vdfText = File.ReadAllText(path);
            var root = VdfConvert.Deserialize(vdfText);

            if (root.Value is not VObject usersObj)
            {
                return;
            }

            foreach (var userProp in usersObj)
            {
                if (userProp.Value is not VObject userDetails)
                {
                    continue;
                }

                if (userDetails.ContainsKey("MostRec"))
                {
                    userDetails["MostRec"] = new VValue("0");
                }

                if (userProp.Key != targetSteamId)
                {
                    continue;
                }

                userDetails["MostRec"] = new VValue("1");
                userDetails["RememberPassword"] = new VValue("1");

                if (personaState == -1)
                {
                    continue;
                }

                var wantsOffline = personaState == 0 ? "1" : "0";
                userDetails["WantsOfflineMode"] = new VValue(wantsOffline);
                userDetails["SkipOfflineModeWarning"] = new VValue(wantsOffline);
            }

            File.WriteAllText(path, root.ToString());
        }
        catch (Exception ex)
        {
            Logger.Error($"Error patching vdf: {ex}");
        }
    }

    private void SetPersonaState(string steamId64, int state)
    {
        try
        {
            if (string.IsNullOrEmpty(steamId64))
            {
                return;
            }

            var id32 = new SteamId(steamId64).Id32;
            var steamPath = SteamRegistryHelper.GetSteamPath();
            if (string.IsNullOrEmpty(steamPath))
            {
                return;
            }

            steamPath = steamPath.Replace('/', Path.DirectorySeparatorChar);
            var localConfigPath = Path.Combine(steamPath, "userdata", id32, "config", "localconfig.vdf");
            if (!File.Exists(localConfigPath))
            {
                return;
            }

            var localConfigText = File.ReadAllText(localConfigPath);
            var root = VdfConvert.Deserialize(localConfigText);

            if (root.Value is not VObject rootObj ||
                !rootObj.ContainsKey("friends") ||
                rootObj["friends"] is not VObject friendsObj)
            {
                return;
            }

            friendsObj["ePersonaState"] = new VValue(state.ToString());
            File.WriteAllText(localConfigPath, VdfConvert.Serialize(root));
        }
        catch (Exception ex)
        {
            Logger.Error($"Error setting persona state: {ex.Message}");
        }
    }

    public void ClearCache()
    {
        _imageCache.ClearCache();
        _vacCache.ClearCache();
    }
}