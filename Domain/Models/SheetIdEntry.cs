namespace AppParaUniversidad.Domain.Models;

public sealed class SheetIdEntry
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SheetId { get; set; } = string.Empty;
    public string CreatedAtUtc { get; set; } = string.Empty;
}
