using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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

public sealed class UpdateCheckResult
{
    public bool IsSuccessful { get; init; }
    public bool UpdateAvailable { get; init; }
    public string StatusText { get; init; } = "No se pudo verificar";
    public GitHubUpdateInfo? Release { get; init; }
}

public sealed class UpdateApplyResult
{
    public bool Started { get; init; }
    public bool RequiresShutdown { get; init; }
    public bool OpenReleasePage { get; init; }
    public string Message { get; init; } = string.Empty;
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

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion)
    {
        try
        {
            var latest = await GetLatestReleaseAsync();
            if (latest is null || string.IsNullOrWhiteSpace(latest.TagName))
            {
                return new UpdateCheckResult
                {
                    IsSuccessful = false,
                    StatusText = "No se pudo verificar"
                };
            }

            var hasUpdate = IsRemoteNewer(currentVersion, latest.TagName);
            return new UpdateCheckResult
            {
                IsSuccessful = true,
                UpdateAvailable = hasUpdate,
                StatusText = hasUpdate
                    ? $"Actualizacion disponible ({NormalizeTag(latest.TagName)})"
                    : "Actualizado",
                Release = latest
            };
        }
        catch
        {
            return new UpdateCheckResult
            {
                IsSuccessful = false,
                StatusText = "No se pudo verificar"
            };
        }
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

    public async Task<UpdateApplyResult> ApplyUpdateAsync(GitHubUpdateInfo release)
    {
        if (release is null)
        {
            return new UpdateApplyResult
            {
                Message = "No se encontro la informacion del release."
            };
        }

        if (string.IsNullOrWhiteSpace(release.AssetDownloadUrl))
        {
            return new UpdateApplyResult
            {
                OpenReleasePage = true,
                Message = "El release no contiene un archivo descargable."
            };
        }

        var tempRoot = Path.Combine(Path.GetTempPath(), "ApoyoDocentesUpdater", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(tempRoot);

        var assetName = string.IsNullOrWhiteSpace(release.AssetName) ? "update.bin" : release.AssetName;
        var assetPath = Path.Combine(tempRoot, assetName);
        await DownloadFileAsync(release.AssetDownloadUrl, assetPath);

        var extension = Path.GetExtension(assetPath).ToLowerInvariant();
        if (extension == ".msi" || extension == ".exe")
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = assetPath,
                UseShellExecute = true
            });

            return new UpdateApplyResult
            {
                Started = true,
                Message = "Instalador abierto."
            };
        }

        if (extension != ".zip")
        {
            return new UpdateApplyResult
            {
                OpenReleasePage = true,
                Message = "Formato de actualizacion no soportado automaticamente."
            };
        }

        var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(currentExe))
        {
            return new UpdateApplyResult
            {
                Message = "No se pudo detectar el ejecutable actual."
            };
        }

        var appDir = Path.GetDirectoryName(currentExe);
        if (string.IsNullOrWhiteSpace(appDir))
        {
            return new UpdateApplyResult
            {
                Message = "No se pudo detectar la carpeta de la aplicacion."
            };
        }

        var exeName = Path.GetFileName(currentExe);
        var extractDir = Path.Combine(tempRoot, "extract");
        ZipFile.ExtractToDirectory(assetPath, extractDir, true);

        var updaterScript = Path.Combine(tempRoot, "apply-update.cmd");
        var script =
            "@echo off" + Environment.NewLine +
            "timeout /t 2 /nobreak > nul" + Environment.NewLine +
            $"robocopy \"{extractDir}\" \"{appDir}\" /E /R:2 /W:1 > nul" + Environment.NewLine +
            $"start \"\" \"{Path.Combine(appDir, exeName)}\"" + Environment.NewLine +
            "exit /b 0";

        File.WriteAllText(updaterScript, script);

        Process.Start(new ProcessStartInfo
        {
            FileName = updaterScript,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });

        return new UpdateApplyResult
        {
            Started = true,
            RequiresShutdown = true,
            Message = "Actualizacion descargada."
        };
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

    public static string NormalizeTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return string.Empty;
        }

        return tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? tag : $"v{tag}";
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ApoyoDocentes", "1.0"));
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
