using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AppParaUniversidad.Common;
using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Schedules;

public static class ScheduleParser
{
    private static readonly Regex TimeRegex = new(@"\b\d{1,2}:\d{2}\s*(?:am|pm|a\.m\.|p\.m\.)?\b", RegexOptions.IgnoreCase);

    private static readonly Dictionary<string, string> DayMap = new()
    {
        { "lunes", "Lunes" },
        { "lun", "Lunes" },
        { "martes", "Martes" },
        { "mar", "Martes" },
        { "miercoles", "Miercoles" },
        { "mier", "Miercoles" },
        { "mi", "Miercoles" },
        { "jueves", "Jueves" },
        { "jue", "Jueves" },
        { "viernes", "Viernes" },
        { "vie", "Viernes" },
        { "sabado", "Sabado" },
        { "sab", "Sabado" },
        { "domingo", "Domingo" },
        { "dom", "Domingo" },
    };

    public static bool TryParseSlotHeader(string header, out TimeSpan start, out TimeSpan end)
    {
        start = default;
        end = default;

        if (string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        var matches = TimeRegex.Matches(header);
        if (matches.Count == 0)
        {
            return false;
        }

        if (!TryParseTime(matches[0].Value, out start))
        {
            return false;
        }

        if (matches.Count > 1 && TryParseTime(matches[1].Value, out var parsedEnd))
        {
            end = parsedEnd;
            return true;
        }

        end = start.Add(TimeSpan.FromHours(1));
        return true;
    }

    public static List<string> ParseDays(string raw)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return result;
        }

        var parts = raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var token = NameNormalizer.Normalize(part);
            if (DayMap.TryGetValue(token, out var day))
            {
                if (!result.Contains(day))
                {
                    result.Add(day);
                }
            }
        }

        return result;
    }

    public static void CompactAndCalculate(TeacherSchedule schedule)
    {
        schedule.BloquesPorDia.Clear();
        schedule.HorasPorDia.Clear();
        schedule.TotalSemanalHoras = 0;

        foreach (var kvp in schedule.SlotsPorDia)
        {
            var day = kvp.Key;
            var slots = kvp.Value;
            slots.Sort((a, b) => a.Start.CompareTo(b.Start));
            var blocks = CompactRanges(slots);
            schedule.BloquesPorDia[day] = blocks;

            var total = 0.0;
            foreach (var block in blocks)
            {
                total += (block.End - block.Start).TotalHours;
            }

            schedule.HorasPorDia[day] = total;
            schedule.TotalSemanalHoras += total;
        }
    }

    public static List<TimeRange> CompactRanges(List<TimeRange> ranges)
    {
        var result = new List<TimeRange>();
        if (ranges.Count == 0)
        {
            return result;
        }

        var current = ranges[0];
        for (var i = 1; i < ranges.Count; i++)
        {
            var next = ranges[i];
            if (next.Start <= current.End)
            {
                var end = next.End > current.End ? next.End : current.End;
                current = new TimeRange(current.Start, end);
                continue;
            }

            result.Add(current);
            current = next;
        }

        result.Add(current);
        return result;
    }

    private static bool TryParseTime(string input, out TimeSpan time)
    {
        time = default;
        var cleaned = input.Trim().ToLowerInvariant()
            .Replace("a.m.", "am")
            .Replace("p.m.", "pm");

        if (DateTime.TryParseExact(cleaned,
                ["H:mm", "HH:mm", "h:mm tt", "hh:mm tt"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
        {
            time = dt.TimeOfDay;
            return true;
        }

        if (DateTime.TryParse(cleaned, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            time = dt.TimeOfDay;
            return true;
        }

        return false;
    }

    public static bool TryParsePriority(string raw, out string priority)
    {
        priority = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = NameNormalizer.Normalize(raw);
        if (normalized == "alta")
        {
            priority = "Alta";
            return true;
        }

        if (normalized == "media")
        {
            priority = "Media";
            return true;
        }

        if (normalized == "baja")
        {
            priority = "Baja";
            return true;
        }

        return false;
    }
}





