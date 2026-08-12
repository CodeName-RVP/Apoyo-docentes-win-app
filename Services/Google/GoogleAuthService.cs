using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppParaUniversidad.Services.Security;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;

namespace AppParaUniversidad.Services.Google;

public sealed class GoogleAuthService
{
    private const string AppFolder = "AppParaUniversidad";
    private const string TokenFolderName = "tokens-google";

    private const string ClientId =
        "CONFIGURE_GOOGLE_CLIENT_ID";

    private const string ClientSecret =
        "CONFIGURE_GOOGLE_CLIENT_SECRET";

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
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appPath = Path.Combine(appData, AppFolder);
        System.IO.Directory.CreateDirectory(appPath);

        var tokenDir = Path.Combine(appPath, TokenFolderName);
        System.IO.Directory.CreateDirectory(tokenDir);

        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret
            },
            DataStore = new DpapiDataStore(tokenDir)
        };

        return await GoogleWebAuthorizationBroker.AuthorizeAsync(
            initializer,
            Scopes,
            "user",
            usePkce: true,
            CancellationToken.None).ConfigureAwait(false);
    }

    public Task<UserCredential> GetCredentialAsync()
        => _credentialTask.Value;

    public async Task InitializeAsync(CancellationToken ct = default)
        => await _credentialTask.Value.WaitAsync(ct).ConfigureAwait(false);
}