using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppParaUniversidad.Services.Updates;

public sealed class GitHubUpdateInfo
{
    public string TagName { get; init; } = string.Empty;
    public string HtmlUrl { get; init; } = string.Empty;
    public string AssetName { get; init; } = string.Empty;
    public string AssetDownloadUrl { get; init; } = string.Empty;
}

public sealed class GitHubUpdateService
{
    private const string LatestReleaseEndpoint = "https://api.github.com/repos/CodeName-RVP/Apoyo-docentes-win-app/releases/latest";

    public async Task<GitHubUpdateInfo?> GetLatestReleaseAsync()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync(LatestReleaseEndpoint);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        var tag = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? string.Empty : string.Empty;
        var html = root.TryGetProperty("html_url", out var htmlProp) ? htmlProp.GetString() ?? string.Empty : string.Empty;

        var (assetName, assetUrl) = ReadPreferredAsset(root);

        return new GitHubUpdateInfo
        {
            TagName = tag,
            HtmlUrl = html,
            AssetName = assetName,
            AssetDownloadUrl = assetUrl
        };
    }

    public async Task DownloadFileAsync(string url, string destinationPath)
    {
        using var client = CreateClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync();
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination);
    }

    public static bool IsRemoteNewer(string currentVersion, string remoteTag)
    {
        var current = ParseVersion(currentVersion);
        var remote = ParseVersion(remoteTag);
        if (current is null || remote is null)
        {
            return false;
        }

        return remote > current;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AppParaUniversidad", "1.0"));
        return client;
    }

    private static (string Name, string Url) ReadPreferredAsset(JsonElement root)
    {
        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return (string.Empty, string.Empty);
        }

        var candidates = assets.EnumerateArray()
            .Select(a =>
            {
                var name = a.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                var url = a.TryGetProperty("browser_download_url", out var u) ? u.GetString() ?? string.Empty : string.Empty;
                return (name, url);
            })
            .Where(a => !string.IsNullOrWhiteSpace(a.name) && !string.IsNullOrWhiteSpace(a.url))
            .ToList();

        if (candidates.Count == 0)
        {
            return (string.Empty, string.Empty);
        }

        foreach (var ext in new[] { ".zip", ".msi", ".exe" })
        {
            var match = candidates.FirstOrDefault(c => c.name.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match.name))
            {
                return (match.name, match.url);
            }
        }

        var first = candidates[0];
        return (first.name, first.url);
    }

    private static Version? ParseVersion(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var clean = input.Trim();
        if (clean.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean[1..];
        }

        return Version.TryParse(clean, out var version) ? version : null;
    }
}
