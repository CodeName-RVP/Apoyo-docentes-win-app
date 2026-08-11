using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppParaUniversidad.Services.Updates;

public sealed class GitHubUpdateInfo
{
    public string TagName { get; init; } = string.Empty;
    public string HtmlUrl { get; init; } = string.Empty;
    public string AssetName { get; init; } = string.Empty;
    public string AssetDownloadUrl { get; init; } = string.Empty;
    public string AssetSha256 { get; init; } = string.Empty;
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
    private const string RepoOwner = "CodeName-RVP";
    private const string RepoName = "Apoyo-docentes-win-app";
    private const string RepoApiBase = "https://api.github.com/repos/CodeName-RVP/Apoyo-docentes-win-app";
    private const string LatestReleaseEndpoint = RepoApiBase + "/releases/latest";
    private const string TagsEndpoint = RepoApiBase + "/tags";
    private const string ReleasesPageUrl = "https://github.com/CodeName-RVP/Apoyo-docentes-win-app/releases";

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
        var (assetName, assetUrl, assetSha256) = ReadPreferredAsset(root);

        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        return new GitHubUpdateInfo
        {
            TagName = tag,
            HtmlUrl = string.IsNullOrWhiteSpace(html) ? ReleasesPageUrl : html,
            AssetName = assetName,
            AssetDownloadUrl = assetUrl,
            AssetSha256 = assetSha256
        };
    }

    public async Task<GitHubUpdateInfo?> GetLatestTagAsync()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync(TagsEndpoint);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
        {
            return null;
        }

        var first = doc.RootElement[0];
        var tag = first.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        return new GitHubUpdateInfo
        {
            TagName = tag,
            HtmlUrl = ReleasesPageUrl,
            AssetName = string.Empty,
            AssetDownloadUrl = string.Empty
        };
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion)
    {
        try
        {
            var latest = await GetLatestReleaseAsync();
            if (latest is not null)
            {
                return BuildCheckResult(currentVersion, latest);
            }

            latest = await GetLatestTagAsync();
            if (latest is not null)
            {
                return BuildCheckResult(currentVersion, latest);
            }

            return new UpdateCheckResult
            {
                IsSuccessful = false,
                StatusText = "No se pudo verificar"
            };
        }
        catch (Exception ex)
        {
            Common.Logger.LogError(nameof(CheckForUpdatesAsync), ex);
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
                Message = "No se encontro un paquete descargable. Se abrira la pagina de releases."
            };
        }

        if (string.IsNullOrWhiteSpace(release.AssetSha256))
        {
            return new UpdateApplyResult
            {
                OpenReleasePage = true,
                Message = "El release no publica un hash SHA-256 verificable. La actualizacion automatica se bloqueo por seguridad."
            };
        }

        var tempRoot = Path.Combine(Path.GetTempPath(), "ApoyoDocentesUpdater", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(tempRoot);

        var assetName = string.IsNullOrWhiteSpace(release.AssetName) ? "update.bin" : release.AssetName;
        var assetPath = Path.Combine(tempRoot, assetName);
        await DownloadFileAsync(release.AssetDownloadUrl, assetPath);

        if (!await VerifySha256Async(assetPath, release.AssetSha256))
        {
            return new UpdateApplyResult
            {
                Message = "La verificacion SHA-256 del paquete fallo. No se aplico la actualizacion."
            };
        }

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

    private static UpdateCheckResult BuildCheckResult(string currentVersion, GitHubUpdateInfo latest)
    {
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

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ApoyoDocentes", "1.0"));
        return client;
    }

    private static async Task<bool> VerifySha256Async(string path, string expectedDigest)
    {
        const string prefix = "sha256:";
        if (!expectedDigest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var expected = expectedDigest[prefix.Length..];
        if (expected.Length != 64 || expected.Any(c => !Uri.IsHexDigit(c)))
        {
            return false;
        }

        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream);
        return string.Equals(Convert.ToHexString(hash), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static (string Name, string Url, string Sha256) ReadPreferredAsset(JsonElement root)
    {
        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return (string.Empty, string.Empty, string.Empty);
        }

        var candidates = assets.EnumerateArray()
            .Select(a =>
            {
                var name = a.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                var url = a.TryGetProperty("browser_download_url", out var u) ? u.GetString() ?? string.Empty : string.Empty;
                var digest = a.TryGetProperty("digest", out var d) ? d.GetString() ?? string.Empty : string.Empty;
                return (name, url, digest);
            })
            .Where(a => !string.IsNullOrWhiteSpace(a.name) && !string.IsNullOrWhiteSpace(a.url))
            .ToList();

        if (candidates.Count == 0)
        {
            return (string.Empty, string.Empty, string.Empty);
        }

        foreach (var ext in new[] { ".zip", ".msi", ".exe" })
        {
            var match = candidates.FirstOrDefault(c => c.name.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match.name))
            {
                return (match.name, match.url, match.digest);
            }
        }

        var first = candidates[0];
        return (first.name, first.url, first.digest);
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
