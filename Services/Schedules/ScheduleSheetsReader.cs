using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using AppParaUniversidad.Common;
using AppParaUniversidad.Domain.Models;
using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;

namespace AppParaUniversidad.Services.Schedules;

public sealed class ScheduleSheetsReader : IScheduleReader
{
    private const string AppFolder = "AppParaUniversidad";
    private const string CredentialsFileName = "client_secret.json";
    private const string TokenFolderName = "tokens_sheets";
    private readonly Lazy<Task<SheetsService>> _serviceTask;

    public ScheduleSheetsReader()
    {
        _serviceTask = new Lazy<Task<SheetsService>>(BuildServiceAsync);
    }

    public ScheduleReadResult Read(string filePath, string sheetName)
    {
        var result = new ScheduleReadResult();
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.Errors.Add("El ID de la hoja es requerido.");
                return result;
            }

            var service = GetService();
            var range = sheetName;
            var request = service.Spreadsheets.Values.Get(filePath, range);
            var response = request.Execute();
            var values = response.Values;
            if (values is null || values.Count == 0)
            {
                result.Errors.Add("La hoja esta vacia.");
                return result;
            }

            var rows = ToRows(values);
            var headerInfo = DetectHeader(rows, result);
            if (headerInfo is null)
            {
                return result;
            }

            for (var row = headerInfo.HeaderRow + 1; row < rows.Count; row++)
            {
                var rowValues = rows[row];
                if (headerInfo.NameColumn >= rowValues.Length)
                {
                    continue;
                }

                var nombreVisible = rowValues[headerInfo.NameColumn].Trim();
                if (string.IsNullOrWhiteSpace(nombreVisible))
                {
                    continue;
                }

                var schedule = new TeacherSchedule
                {
                    NombreVisible = nombreVisible,
                    NombreNormalizado = NameNormalizer.Normalize(nombreVisible)
                };

                foreach (var slot in headerInfo.Slots)
                {
                    if (slot.ColumnIndex >= rowValues.Length)
                    {
                        continue;
                    }

                    var cellValue = rowValues[slot.ColumnIndex].Trim();
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        continue;
                    }

                    var days = ScheduleParser.ParseDays(cellValue);
                    foreach (var day in days)
                    {
                        if (!schedule.SlotsPorDia.TryGetValue(day, out var list))
                        {
                            list = new List<TimeRange>();
                            schedule.SlotsPorDia[day] = list;
                        }

                        list.Add(new TimeRange(slot.Start, slot.End));
                    }
                }

                foreach (var pref in headerInfo.PreferenceColumns)
                {
                    if (pref.ColumnIndex >= rowValues.Length)
                    {
                        continue;
                    }

                    var value = rowValues[pref.ColumnIndex].Trim();
                    if (ScheduleParser.TryParsePriority(value, out var priority))
                    {
                        schedule.SubjectPreferences.Add(new SubjectPreference
                        {
                            Subject = pref.HeaderText,
                            Priority = priority
                        });
                    }
                }

                ScheduleParser.CompactAndCalculate(schedule);
                result.Schedules.Add(schedule);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(ScheduleSheetsReader), ex);
            result.Errors.Add("Error leyendo Google Sheets.");
        }

        return result;
    }

    public List<string> ListSheetNames(string filePath)
    {
        var names = new List<string>();
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return names;
            }

            var service = GetService();
            var request = service.Spreadsheets.Get(filePath);
            request.Fields = "sheets.properties.title";
            var spreadsheet = request.Execute();
            if (spreadsheet.Sheets is null)
            {
                return names;
            }

            foreach (var sheet in spreadsheet.Sheets)
            {
                var title = sheet.Properties?.Title;
                if (!string.IsNullOrWhiteSpace(title))
                {
                    names.Add(title);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(ListSheetNames), ex);
        }

        return names;
    }

    private SheetsService GetService() => _serviceTask.Value.GetAwaiter().GetResult();

    private async Task<SheetsService> BuildServiceAsync()
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
                new[] { SheetsService.Scope.SpreadsheetsReadonly },
                "user",
                cts.Token,
                new FileDataStore(tokenDir, true)).ConfigureAwait(false);

            return new SheetsService(new BaseClientService.Initializer
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

    
    public List<ContactImportRow> ReadContacts(string sheetId, string sheetName)
    {
        var rows = new List<ContactImportRow>();
        try
        {
            if (string.IsNullOrWhiteSpace(sheetId) || string.IsNullOrWhiteSpace(sheetName))
            {
                return rows;
            }

            var service = GetService();
            var request = service.Spreadsheets.Values.Get(sheetId, sheetName);
            var response = request.Execute();
            var values = response.Values;
            if (values is null || values.Count == 0)
            {
                return rows;
            }

            var rawRows = ToRows(values);
            var header = DetectContactHeader(rawRows);
            if (header is null)
            {
                return rows;
            }

            for (var row = header.HeaderRow + 1; row < rawRows.Count; row++)
            {
                var cols = rawRows[row];
                if (header.NameColumn >= cols.Length || header.EmailColumn >= cols.Length)
                {
                    continue;
                }

                var name = cols[header.NameColumn].Trim();
                var email = cols[header.EmailColumn].Trim();
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                var phone = header.PhoneColumn >= 0 && header.PhoneColumn < cols.Length
                    ? cols[header.PhoneColumn].Trim()
                    : null;

                rows.Add(new ContactImportRow
                {
                    Name = name,
                    Email = email,
                    Phone = phone
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(ReadContacts), ex);
        }

        return rows;
    }

    private sealed class ContactHeaderInfo
    {
        public int HeaderRow { get; init; }
        public int NameColumn { get; init; }
        public int EmailColumn { get; init; }
        public int PhoneColumn { get; init; }
    }

    private static ContactHeaderInfo? DetectContactHeader(List<string[]> rows)
    {
        var maxRow = Math.Min(50, rows.Count);
        for (var row = 0; row < maxRow; row++)
        {
            var values = rows[row];
            var nameCol = -1;
            var emailCol = -1;
            var phoneCol = -1;

            for (var col = 0; col < values.Length; col++)
            {
                var text = values[col].Trim();
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                var normalized = NameNormalizer.Normalize(text);
                if (nameCol < 0 && normalized.Contains("nombre"))
                {
                    nameCol = col;
                    continue;
                }

                if (emailCol < 0 && (normalized.Contains("correo") || normalized.Contains("email")))
                {
                    emailCol = col;
                    continue;
                }

                if (phoneCol < 0 && (normalized.Contains("telefono") || normalized.Contains("celular")))
                {
                    phoneCol = col;
                }
            }

            if (nameCol >= 0 && emailCol >= 0)
            {
                return new ContactHeaderInfo
                {
                    HeaderRow = row,
                    NameColumn = nameCol,
                    EmailColumn = emailCol,
                    PhoneColumn = phoneCol
                };
            }
        }

        return null;
    }private static List<string[]> ToRows(IList<IList<object>> values)
    {
        var rows = new List<string[]>(values.Count);
        var maxCols = values.Max(v => v?.Count ?? 0);
        foreach (var row in values)
        {
            var current = new string[maxCols];
            Array.Fill(current, string.Empty);
            if (row != null)
            {
                for (var i = 0; i < row.Count; i++)
                {
                    current[i] = row[i]?.ToString() ?? string.Empty;
                }
            }

            rows.Add(current);
        }

        return rows;
    }

    private static ScheduleHeaderInfo? DetectHeader(List<string[]> rows, ScheduleReadResult result)
    {
        var maxRow = Math.Min(50, rows.Count);
        for (var row = 0; row < maxRow; row++)
        {
            var values = rows[row];
            var nameColumn = -1;
            var slots = new List<ScheduleSlotHeader>();
            var candidates = new List<SubjectPreferenceColumn>();

            for (var col = 0; col < values.Length; col++)
            {
                var text = values[col].Trim();
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                var normalized = NameNormalizer.Normalize(text);
                if (nameColumn < 0 && (normalized.Contains("nombre") || normalized.Contains("docente")))
                {
                    nameColumn = col;
                    continue;
                }

                if (ScheduleParser.TryParseSlotHeader(text, out var start, out var end))
                {
                    slots.Add(new ScheduleSlotHeader(col, start, end, text));
                    continue;
                }

                candidates.Add(new SubjectPreferenceColumn(col, text));
            }

            if (nameColumn >= 0 && slots.Count > 0)
            {
                var preferenceColumns = DetectPreferenceColumns(rows, row, candidates);

                return new ScheduleHeaderInfo
                {
                    HeaderRow = row,
                    NameColumn = nameColumn,
                    Slots = slots.OrderBy(s => s.Start).ThenBy(s => s.ColumnIndex).ToList(),
                    PreferenceColumns = preferenceColumns
                };
            }
        }

        result.Errors.Add("No se encontro fila de encabezados con columna de nombre y horarios.");
        return null;
    }

    private static List<SubjectPreferenceColumn> DetectPreferenceColumns(
        List<string[]> rows,
        int headerRow,
        List<SubjectPreferenceColumn> candidates)
    {
        var result = new List<SubjectPreferenceColumn>();
        var maxRow = Math.Min(rows.Count - 1, headerRow + 50);

        foreach (var candidate in candidates)
        {
            for (var row = headerRow + 1; row <= maxRow; row++)
            {
                if (candidate.ColumnIndex >= rows[row].Length)
                {
                    continue;
                }

                var value = rows[row][candidate.ColumnIndex].Trim();
                if (ScheduleParser.TryParsePriority(value, out _))
                {
                    result.Add(candidate);
                    break;
                }
            }
        }

        return result;
    }
}






