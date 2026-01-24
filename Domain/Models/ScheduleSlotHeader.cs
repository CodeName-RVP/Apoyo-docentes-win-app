namespace AppParaUniversidad.Domain.Models;

public sealed class ScheduleSlotHeader
{
    public ScheduleSlotHeader(int columnIndex, TimeSpan start, TimeSpan end, string headerText)
    {
        ColumnIndex = columnIndex;
        Start = start;
        End = end;
        HeaderText = headerText;
    }

    public int ColumnIndex { get; }
    public TimeSpan Start { get; }
    public TimeSpan End { get; }
    public string HeaderText { get; }
}
