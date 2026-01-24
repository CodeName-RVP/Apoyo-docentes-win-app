namespace AppParaUniversidad.Domain.Models;

public sealed class SubjectPreferenceColumn
{
    public SubjectPreferenceColumn(int columnIndex, string headerText)
    {
        ColumnIndex = columnIndex;
        HeaderText = headerText;
    }

    public int ColumnIndex { get; }
    public string HeaderText { get; }
}
