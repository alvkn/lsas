namespace LightSteamAccountSwitcher.Core.Models;

public class SteamAccount
{
    // From loginusers.vdf
    public required string SteamId64 { get; set; }

    public required string AccountName { get; set; }

    public required string PersonaName { get; set; }

    public DateTime LastLogin { get; set; } // We might need to handle parsing this from string/long

    public bool WantsOfflineMode { get; set; }

    public bool SkipOfflineModeWarning { get; set; } // Sometimes present

    public bool MostRecent { get; set; }

    public bool RememberPassword { get; set; }

    // From Profile XML / Cache
    public required string AvatarUrl { get; set; }

    public bool IsVacBanned { get; set; }

    public bool IsLimited { get; set; }

    // State
    public bool IsProfileLoaded { get; set; }
}