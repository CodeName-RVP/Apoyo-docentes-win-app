namespace AppParaUniversidad.Domain.Models;

public sealed class ContactImportRow
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
