namespace LightSteamAccountSwitcher.Core.Models;

public class SteamProfile
{
    public required string SteamId64 { get; set; }

    public string? AvatarFullUrl { get; set; }

    public bool VacBanned { get; set; }

    public bool IsLimitedAccount { get; set; }

    public DateTime LastUpdated { get; set; }
}