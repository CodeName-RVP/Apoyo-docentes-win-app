using System.Threading;
using System.Threading.Tasks;
using AppParaUniversidad.Common;

namespace AppParaUniversidad.Services.Mail;

/// <summary>
/// Gmail service de respaldo que no envía y evita que la app crashee si faltan credenciales.
/// </summary>
public sealed class NullGmailService : IGmailService
{
    private readonly string _reason;

    public NullGmailService(string reason)
    {
        _reason = reason;
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, string? ccEmail, CancellationToken ct = default)
    {
        Logger.LogError(nameof(NullGmailService), new InvalidOperationException(_reason));
        throw new InvalidOperationException(_reason);
    }

    public Task<string?> GetAccountEmailAsync(CancellationToken ct = default)
        => Task.FromResult<string?>(null);
}
