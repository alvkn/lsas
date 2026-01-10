using System.Net.Http.Headers;
using System.Text.Json;
using LightSteamAccountSwitcher.Core.Models;

namespace LightSteamAccountSwitcher.Core.Services;

public static class UpdateService
{
    private const string RepoOwner = "alvkn";
    private const string RepoName = "lsas";
    private const string ApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";

    public static async Task<UpdateInfo> CheckForUpdateAsync(string currentVersion)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LightSteamAccountSwitcher",
                currentVersion));

            var response = await client.GetAsync(ApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateInfo { IsAvailable = false };
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);

            if (release == null || string.IsNullOrEmpty(release.TagName))
            {
                return new UpdateInfo { IsAvailable = false };
            }

            var latestTag = release.TagName.TrimStart('v');
            var currentTag = currentVersion.TrimStart('v');

            if (Version.TryParse(latestTag, out var latest) &&
                Version.TryParse(currentTag, out var current) &&
                latest > current)
            {
                return new UpdateInfo
                {
                    IsAvailable = true,
                    ReleaseUrl = $"https://github.com/{RepoOwner}/{RepoName}/releases/latest"
                };
            }
        }
        catch
        {
            // Ignore network errors or parsing errors
        }

        return new UpdateInfo { IsAvailable = false };
    }
}