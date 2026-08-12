using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppParaUniversidad.Services.Google;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using GoogleGmail = Google.Apis.Gmail.v1.GmailService;

namespace AppParaUniversidad.Services.Mail;

public sealed class GmailService : IGmailService
{
    private readonly Lazy<Task<GoogleGmail>> _serviceTask;

    public GmailService()
    {
        _serviceTask = new Lazy<Task<GoogleGmail>>(BuildServiceAsync);
    }

    private async Task<GoogleGmail> BuildServiceAsync()
    {
        var credential = await GoogleAuthService.Shared.GetCredentialAsync().ConfigureAwait(false);

        return new GoogleGmail(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Apoyo Docentes"
        });
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await GoogleAuthService.Shared.InitializeAsync(ct).ConfigureAwait(false);
        await _serviceTask.Value.WaitAsync(ct).ConfigureAwait(false);
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? ccEmail,
        CancellationToken ct = default)
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

        await service.Users.Messages.Send(
            new Message { Raw = raw }, "me").ExecuteAsync(ct);
    }

    public async Task<string?> GetAccountEmailAsync(CancellationToken ct = default)
    {
        var service = await _serviceTask.Value.ConfigureAwait(false);
        var profile = await service.Users.GetProfile("me").ExecuteAsync(ct);
        return profile?.EmailAddress;
    }
}