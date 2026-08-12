using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Google.Apis.Util.Store;

namespace AppParaUniversidad.Services.Security;

/// <summary>
/// Persiste datos OAuth cifrados con DPAPI para el usuario actual de Windows.
/// Tambien migra, al primer uso, los tokens que antes se guardaban con FileDataStore.
/// </summary>
public sealed class DpapiDataStore : IDataStore
{
    private static readonly byte[] Entropy =
        Encoding.UTF8.GetBytes("AppParaUniversidad.OAuthTokens.v1");

    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly string _folderPath;
    private readonly FileDataStore _legacyStore;

    public DpapiDataStore(string folderPath)
    {
        _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));

        System.IO.Directory.CreateDirectory(_folderPath);

        _legacyStore = new FileDataStore(_folderPath, true);
    }

    public async Task StoreAsync<T>(string key, T value)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

        var encrypted = ProtectedData.Protect(
            payload,
            Entropy,
            DataProtectionScope.CurrentUser);

        var destination = GetProtectedPath(key);
        var temporary = destination + ".tmp";

        await System.IO.File.WriteAllBytesAsync(
            temporary,
            encrypted).ConfigureAwait(false);

        System.IO.File.Move(
            temporary,
            destination,
            true);
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var path = GetProtectedPath(key);

        if (System.IO.File.Exists(path))
        {
            var encrypted = await System.IO.File.ReadAllBytesAsync(path)
                .ConfigureAwait(false);

            var payload = ProtectedData.Unprotect(
                encrypted,
                Entropy,
                DataProtectionScope.CurrentUser);

            return JsonSerializer.Deserialize<T>(
                payload,
                SerializerOptions)!;
        }

        var legacy = await _legacyStore
            .GetAsync<T>(key)
            .ConfigureAwait(false);

        if (legacy is null)
        {
            return default!;
        }

        await StoreAsync(key, legacy).ConfigureAwait(false);
        await _legacyStore.DeleteAsync<T>(key).ConfigureAwait(false);

        return legacy;
    }

    public async Task DeleteAsync<T>(string key)
    {
        var path = GetProtectedPath(key);

        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        await _legacyStore.DeleteAsync<T>(key).ConfigureAwait(false);
    }

    public async Task ClearAsync()
    {
        if (System.IO.Directory.Exists(_folderPath))
        {
            foreach (var path in System.IO.Directory.EnumerateFiles(
                _folderPath,
                "*.protected"))
            {
                System.IO.File.Delete(path);
            }
        }

        await _legacyStore.ClearAsync().ConfigureAwait(false);
    }

    private string GetProtectedPath(string key)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(key));

        return System.IO.Path.Combine(
            _folderPath,
            Convert.ToHexString(hash) + ".protected");
    }
}
