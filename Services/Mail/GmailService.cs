using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppParaUniversidad.Common;
using AppParaUniversidad.Services.Security;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GoogleGmail = Google.Apis.Gmail.v1.GmailService;

namespace AppParaUniversidad.Services.Mail;

public sealed class GmailService : IGmailService
{
    private const string AppFolder = "AppParaUniversidad";
    private const string CredentialsFileName = "client_secret.json";
    private const string TokenFolderName = "tokens";
    private readonly Lazy<Task<GoogleGmail>> _serviceTask;

    public GmailService()
    {
        _serviceTask = new Lazy<Task<GoogleGmail>>(BuildServiceAsync);
    }

    private async Task<GoogleGmail> BuildServiceAsync()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appPath = Path.Combine(appData, AppFolder);
        System.IO.Directory.CreateDirectory(appPath);

        var credPath = Path.Combine(appPath, CredentialsFileName);
        if (!File.Exists(credPath))
        {
            throw new InvalidOperationException($"No se encontro {credPath}. Copia tu client_secret.json alli.");
        }

        using var stream = new FileStream(credPath, FileMode.Open, FileAccess.Read);
        var tokenDir = Path.Combine(appPath, TokenFolderName);
        System.IO.Directory.CreateDirectory(tokenDir);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { GoogleGmail.Scope.GmailSend },
                "user",
                cts.Token,
                new DpapiDataStore(tokenDir)).ConfigureAwait(false);

            return new GoogleGmail(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "AppParaUniversidad"
            });
        }
        catch (OperationCanceledException ex)
        {
            Logger.LogError(nameof(BuildServiceAsync), ex);
            throw new InvalidOperationException("Autorizacion cancelada o tiempo agotado.", ex);
        }
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, string? ccEmail, CancellationToken ct = default)
    {
        var service = await _serviceTask.Value.ConfigureAwait(false);
        var msg = new MimeKit.MimeMessage();
        msg.From.Add(new MimeKit.MailboxAddress("", "me"));
        msg.To.Add(MimeKit.MailboxAddress.Parse(toEmail));
        if (!string.IsNullOrWhiteSpace(ccEmail))
        {
            msg.Cc.Add(MimeKit.MailboxAddress.Parse(ccEmail));
        }
        msg.Subject = subject;
        msg.Body = new MimeKit.BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var ms = new MemoryStream();
        await msg.WriteToAsync(ms, ct);
        var raw = Convert.ToBase64String(ms.ToArray())
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var gm = new Message { Raw = raw };
        await service.Users.Messages.Send(gm, "me").ExecuteAsync(ct);
    }

    public async Task<string?> GetAccountEmailAsync(CancellationToken ct = default)
    {
        var service = await _serviceTask.Value.ConfigureAwait(false);
        var profile = await service.Users.GetProfile("me").ExecuteAsync(ct);
        return profile?.EmailAddress;
    }
}
