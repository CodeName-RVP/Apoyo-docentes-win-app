using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppParaUniversidad.Services.Security;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;

namespace AppParaUniversidad.Services.Google;

public sealed class GoogleAuthService
{
    private const string AppFolder = "AppParaUniversidad";
    private const string TokenFolderName = "tokens-google";

    private const string ClientId = GoogleOAuthConfig.ClientId;
    private const string ClientSecret = GoogleOAuthConfig.ClientSecret;
    public static readonly string[] Scopes =
    {
        "https://www.googleapis.com/auth/gmail.send",
        "https://www.googleapis.com/auth/spreadsheets.readonly"
    };

    private static readonly Lazy<GoogleAuthService> _shared =
        new(() => new GoogleAuthService());

    private readonly Lazy<Task<UserCredential>> _credentialTask;

    public static GoogleAuthService Shared => _shared.Value;

    public GoogleAuthService()
    {
        _credentialTask = new Lazy<Task<UserCredential>>(AuthorizeAsync);
    }

    private async Task<UserCredential> AuthorizeAsync()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);

        var appPath = Path.Combine(appData, AppFolder);
        System.IO.Directory.CreateDirectory(appPath);

        var tokenDir = Path.Combine(appPath, TokenFolderName);
        System.IO.Directory.CreateDirectory(tokenDir);

        var dataStore = new DpapiDataStore(tokenDir);

        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret
            }
        };

        try
        {
            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                initializer,
                Scopes,
                "user",
                usePkce: true,
                CancellationToken.None,
                dataStore: dataStore
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Common.Logger.LogError(nameof(AuthorizeAsync), ex);
            throw;
        }
    }

    public Task<UserCredential> GetCredentialAsync()
        => _credentialTask.Value;

    public async Task InitializeAsync(CancellationToken ct = default)
        => await _credentialTask.Value.WaitAsync(ct).ConfigureAwait(false);
}