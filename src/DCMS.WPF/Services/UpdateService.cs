using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace DCMS.WPF.Services;

public class UpdateInfo
{
    public bool IsUpdateAvailable { get; set; }
    public string LatestVersion { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
}

public class UpdateService
{
    private const string Owner = "MohamedGamal-Ahmed";
    private const string Repo = "DCMS";
    
    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        var result = new UpdateInfo();
        
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DCMS-Client");

            var url = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
            var response = await client.GetFromJsonAsync<GitHubRelease>(url);

            if (response != null && !string.IsNullOrEmpty(response.TagName))
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
                // Parse version string (handle v1.1.1, V1.1.1, or 1.1.1)
                var latestVersionStr = response.TagName.TrimStart('v', 'V');
                
                if (Version.TryParse(latestVersionStr, out var latestVersion))
                {
                    result.LatestVersion = latestVersion.ToString();
                    result.ReleaseNotes = response.Body;

                    Debug.WriteLine($"[Update] Comparing: Current={currentVersion}, Latest={latestVersion}");
                    
                    // Try to find an EXE asset
                    var asset = response.Assets?.FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
                    result.DownloadUrl = asset?.BrowserDownloadUrl ?? response.HtmlUrl;

                    // Improved comparison: 1.1.1 should be same as 1.1.1.0
                    // If latest is 3 parts (1.1.1) and current is 4 parts (1.1.1.0), 
                    // latest > current would be FALSE in .NET because 0 > -1.
                    // We normalize by ignoring the 4th part if it's 0 or -1.
                    
                    bool isNewer = false;
                    if (latestVersion.Major > currentVersion.Major) isNewer = true;
                    else if (latestVersion.Major == currentVersion.Major)
                    {
                        if (latestVersion.Minor > currentVersion.Minor) isNewer = true;
                        else if (latestVersion.Minor == currentVersion.Minor)
                        {
                            int latestBuild = latestVersion.Build == -1 ? 0 : latestVersion.Build;
                            int currentBuild = currentVersion.Build == -1 ? 0 : currentVersion.Build;
                            
                            if (latestBuild > currentBuild) isNewer = true;
                            else if (latestBuild == currentBuild)
                            {
                                int latestRev = latestVersion.Revision == -1 ? 0 : latestVersion.Revision;
                                int currentRev = currentVersion.Revision == -1 ? 0 : currentVersion.Revision;
                                if (latestRev > currentRev) isNewer = true;
                            }
                        }
                    }

                    if (isNewer)
                    {
                        result.IsUpdateAvailable = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update Check Failed: {ex.Message}");
        }

        return result;
    }

    // Helper classes for JSON deserialization
    private class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }

    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();
    }
}
