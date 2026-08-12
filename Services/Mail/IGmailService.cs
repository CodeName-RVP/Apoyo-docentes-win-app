using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Mail;

public interface IGmailService
{
    Task InitializeAsync(CancellationToken ct = default);
    Task SendAsync(string toEmail, string subject, string htmlBody, string? ccEmail, CancellationToken ct = default);
    Task<string?> GetAccountEmailAsync(CancellationToken ct = default);
}
