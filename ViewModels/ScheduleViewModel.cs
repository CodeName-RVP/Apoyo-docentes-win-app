using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using AppParaUniversidad.Common;
using AppParaUniversidad.Domain.Models;
using AppParaUniversidad.Services.Schedules;
using AppParaUniversidad.Services.Directory;
using AppParaUniversidad.Services.Sheets;
using System.Windows;
using Microsoft.Win32;

namespace AppParaUniversidad.ViewModels;

public sealed class ScheduleDaySummary
{
    public string Day { get; set; } = string.Empty;
    public string RangesText { get; set; } = string.Empty;
    public double Hours { get; set; }
}

public class ScheduleViewModel : INotifyPropertyChanged
{
    private static readonly string[] DayOrder =
    [
        "Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado", "Domingo"
    ];

    private const string SourceFile = "Archivo local";
    private const string SourceSheets = "Google Sheets";

    private readonly IScheduleReader _fileReader;
    private readonly IScheduleReader _sheetReader;
    private readonly ISheetHistoryRepository _sheetHistoryRepository;
    private readonly IDirectoryRepository _directoryRepository;
    private readonly AsyncRelayCommand _analyzeCommand;
    private readonly AsyncRelayCommand _loadGoogleSheetsCommand;
    private DispatcherTimer? _timer;
    private bool _isRefreshing;
    private string? _excelPath;
    private string? _selectedSheet;
    private string _selectedSource = SourceFile;
    private string _googleSheetId = string.Empty;
    private bool _showSaveSheetPrompt;
    private string _sheetNameToSave = string.Empty;
    private string _sheetAliasEdit = string.Empty;
    private SheetIdEntry? _selectedSheetHistory;
    private bool _contactSyncOffered;
    private string _status = "Seleccione archivo";
    private TeacherSchedule? _selectedSchedule;
    private bool _autoRefreshEnabled;
    private int _selectedRefreshInterval = 5;
    private string _lastRefreshText = "Sin actualizacion";
    private string _searchText = string.Empty;
    private int _visibleTeachers;
    private double _averageHours;
    private string _preferenceSummary = "Sin materias registradas";

    public ObservableCollection<string> SheetNames { get; } = new();
    public ObservableCollection<SheetIdEntry> SheetHistory { get; } = new();
    public ObservableCollection<TeacherSchedule> Schedules { get; } = new();
    public ICollectionView SchedulesView { get; }
    public ObservableCollection<ScheduleDaySummary> DaySummaries { get; } = new();
    public ObservableCollection<SubjectPreference> SelectedPreferences { get; } = new();
    public ObservableCollection<string> Errors { get; } = new();
    public ObservableCollection<int> RefreshIntervals { get; } = new() { 1, 5, 10, 30 };
    public ObservableCollection<string> SourceOptions { get; } = new() { SourceFile, SourceSheets };

    public ICommand OpenFileCommand { get; }
    public ICommand AnalyzeCommand => _analyzeCommand;
    public ICommand LoadGoogleSheetsCommand => _loadGoogleSheetsCommand;
    public ICommand SaveSheetIdCommand { get; }
    public ICommand CancelSaveSheetIdCommand { get; }
    public ICommand ShowSavePromptCommand { get; }
    public ICommand SaveAliasCommand { get; }
    public ICommand ImportSheetIdsCommand { get; }
    public ICommand ExportSheetIdsCommand { get; }
    public ICommand UseSheetIdCommand { get; }
    public ICommand DeleteSheetIdCommand { get; }
    public ICommand DeleteSheetIdItemCommand { get; }

    public string? SelectedSheet
    {
        get => _selectedSheet;
        set
        {
            if (_selectedSheet != value)
            {
                _selectedSheet = value;
                OnPropertyChanged();
                _analyzeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SelectedSource
    {
        get => _selectedSource;
        set
        {
            if (_selectedSource != value)
            {
                _selectedSource = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFileSource));
                OnPropertyChanged(nameof(IsGoogleSource));
                _analyzeCommand.RaiseCanExecuteChanged();
                _loadGoogleSheetsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsFileSource => SelectedSource == SourceFile;

    public bool IsGoogleSource => SelectedSource == SourceSheets;

    public string GoogleSheetId
    {
        get => _googleSheetId;
        set
        {
            if (_googleSheetId != value)
            {
                _googleSheetId = value;
                OnPropertyChanged();
                _analyzeCommand.RaiseCanExecuteChanged();
                _loadGoogleSheetsCommand.RaiseCanExecuteChanged();
                if (ShowSavePromptCommand is RelayCommand rpc) rpc.RaiseCanExecuteChanged();
            }
        }
    }

    public SheetIdEntry? SelectedSheetHistory
    {
        get => _selectedSheetHistory;
        set
        {
            if (_selectedSheetHistory != value)
            {
                _selectedSheetHistory = value;
                OnPropertyChanged();
                if (_selectedSheetHistory is not null)
                {
                    GoogleSheetId = _selectedSheetHistory.SheetId;
                    SheetAliasEdit = _selectedSheetHistory.Name;
                }
                if (UseSheetIdCommand is AsyncRelayCommand useCmd) useCmd.RaiseCanExecuteChanged();
                if (DeleteSheetIdCommand is AsyncRelayCommand delCmd) delCmd.RaiseCanExecuteChanged();
                if (SaveAliasCommand is AsyncRelayCommand aliasCmd) aliasCmd.RaiseCanExecuteChanged();
            }
        }
    }

    public string SheetNameToSave
    {
        get => _sheetNameToSave;
        set
        {
            if (_sheetNameToSave != value)
            {
                _sheetNameToSave = value;
                OnPropertyChanged();
                if (SaveSheetIdCommand is AsyncRelayCommand saveCmd) saveCmd.RaiseCanExecuteChanged();
            }
        }
    }

    public bool ShowSaveSheetPrompt
    {
        get => _showSaveSheetPrompt;
        set
        {
            if (_showSaveSheetPrompt != value)
            {
                _showSaveSheetPrompt = value;
                OnPropertyChanged();
            }
        }
    }

    public string SheetAliasEdit
    {
        get => _sheetAliasEdit;
        set
        {
            if (_sheetAliasEdit != value)
            {
                _sheetAliasEdit = value;
                OnPropertyChanged();
                if (SaveAliasCommand is AsyncRelayCommand aliasCmd) aliasCmd.RaiseCanExecuteChanged();
            }
        }
    }

    public string? ExcelPath
    {
        get => _excelPath;
        private set
        {
            if (_excelPath != value)
            {
                _excelPath = value;
                OnPropertyChanged();
                _analyzeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public TeacherSchedule? SelectedSchedule
    {
        get => _selectedSchedule;
        set
        {
            if (_selectedSchedule != value)
            {
                _selectedSchedule = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedTeacherName));
                OnPropertyChanged(nameof(SelectedTeacherTotalHours));
                RefreshDaySummaries();
                RefreshPreferences();
            }
        }
    }

    public string SelectedTeacherName => SelectedSchedule?.NombreVisible ?? "Sin seleccion";

    public double SelectedTeacherTotalHours => SelectedSchedule?.TotalSemanalHoras ?? 0;

    public string PreferenceSummary
    {
        get => _preferenceSummary;
        private set
        {
            if (_preferenceSummary != value)
            {
                _preferenceSummary = value;
                OnPropertyChanged();
            }
        }
    }

    public int TotalTeachers => Schedules.Count;

    public int VisibleTeachers
    {
        get => _visibleTeachers;
        private set
        {
            if (_visibleTeachers != value)
            {
                _visibleTeachers = value;
                OnPropertyChanged();
            }
        }
    }

    public double AverageHours
    {
        get => _averageHours;
        private set
        {
            if (Math.Abs(_averageHours - value) > 0.01)
            {
                _averageHours = value;
                OnPropertyChanged();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                SchedulesView.Refresh();
                UpdateVisibleTeachers();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    public bool AutoRefreshEnabled
    {
        get => _autoRefreshEnabled;
        set
        {
            if (_autoRefreshEnabled != value)
            {
                _autoRefreshEnabled = value;
                OnPropertyChanged();
                ConfigureTimer();
            }
        }
    }

    public int SelectedRefreshInterval
    {
        get => _selectedRefreshInterval;
        set
        {
            if (_selectedRefreshInterval != value)
            {
                _selectedRefreshInterval = value;
                OnPropertyChanged();
                ConfigureTimer();
            }
        }
    }

    public string LastRefreshText
    {
        get => _lastRefreshText;
        private set
        {
            if (_lastRefreshText != value)
            {
                _lastRefreshText = value;
                OnPropertyChanged();
            }
        }
    }

    public ScheduleViewModel(IScheduleReader fileReader, IScheduleReader sheetReader, ISheetHistoryRepository sheetHistoryRepository, IDirectoryRepository directoryRepository)
    {
        _fileReader = fileReader;
        _sheetReader = sheetReader;
        _sheetHistoryRepository = sheetHistoryRepository;
        _directoryRepository = directoryRepository;
        _sheetHistoryRepository.InitializeAsync().GetAwaiter().GetResult();
        _directoryRepository.InitializeAsync().GetAwaiter().GetResult();

        OpenFileCommand = new RelayCommand(_ => OpenFile());
        _analyzeCommand = new AsyncRelayCommand(AnalyzeAsync, CanAnalyze);
        _loadGoogleSheetsCommand = new AsyncRelayCommand(LoadGoogleSheetsAsync, CanLoadGoogleSheets);
        SaveSheetIdCommand = new AsyncRelayCommand(SaveSheetIdAsync, CanSaveSheetId);
        CancelSaveSheetIdCommand = new RelayCommand(_ => CancelSaveSheetPrompt());
        ShowSavePromptCommand = new RelayCommand(_ => ShowSaveSheetPrompt = true, _ => !string.IsNullOrWhiteSpace(GoogleSheetId));
        SaveAliasCommand = new AsyncRelayCommand(SaveAliasAsync, CanSaveAlias);
        ImportSheetIdsCommand = new AsyncRelayCommand(ImportSheetIdsAsync);
        ExportSheetIdsCommand = new AsyncRelayCommand(ExportSheetIdsAsync);
        UseSheetIdCommand = new AsyncRelayCommand(UseSelectedSheetIdAsync, () => SelectedSheetHistory is not null);
        DeleteSheetIdCommand = new AsyncRelayCommand(DeleteSelectedSheetIdAsync, () => SelectedSheetHistory is not null);
        DeleteSheetIdItemCommand = new RelayCommand(param => _ = DeleteSheetIdByItemAsync(param as SheetIdEntry));

        SchedulesView = CollectionViewSource.GetDefaultView(Schedules);
        SchedulesView.Filter = FilterSchedule;
        Schedules.CollectionChanged += OnSchedulesChanged;

        _ = LoadSheetHistoryAsync();
    }

    private void OnSchedulesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TotalTeachers));
        UpdateVisibleTeachers();
    }

    private bool FilterSchedule(object? item)
    {
        if (item is not TeacherSchedule schedule)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        var search = NameNormalizer.Normalize(SearchText);
        var name = NameNormalizer.Normalize(schedule.NombreVisible);
        return name.Contains(search);
    }

    private void OpenFile()
    {
        SelectedSource = SourceFile;
        var dlg = new OpenFileDialog
        {
            Filter = "Excel (*.xlsx;*.xls)|*.xlsx;*.xls|CSV (*.csv)|*.csv|Todos los archivos|*.*",
            Multiselect = false
        };
        if (dlg.ShowDialog() != true) return;

        _ = LoadFileAsync(dlg.FileName);
    }

    public async Task LoadFileAsync(string path)
    {
        try
        {
            SelectedSource = SourceFile;
            ExcelPath = path;
            SheetNames.Clear();
            Errors.Clear();
            Schedules.Clear();
            DaySummaries.Clear();
            SelectedPreferences.Clear();
            PreferenceSummary = "Sin materias registradas";
            SelectedSchedule = null;
            ShowSaveSheetPrompt = false;
            SheetNameToSave = string.Empty;
            _contactSyncOffered = false;

            var names = await Task.Run(() => _fileReader.ListSheetNames(path));
            foreach (var name in names)
            {
                SheetNames.Add(name);
            }

            if (SheetNames.Count == 1)
            {
                SelectedSheet = SheetNames[0];
            }

            Status = SheetNames.Count > 0 ? "Seleccione hoja" : "Sin hojas detectadas";
            await OfferSavePromptIfNeededAsync();
        }
        catch (Exception ex)
        {
            Status = "Error al cargar archivo";
            Errors.Clear();
            Errors.Add(ex.Message);
            Logger.LogError(nameof(LoadFileAsync), ex);
        }
    }

    public async Task LoadGoogleSheetsAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(GoogleSheetId))
            {
                Status = "Ingresa el ID de la hoja";
                return;
            }

            SelectedSource = SourceSheets;
            SheetNames.Clear();
            Errors.Clear();
            Schedules.Clear();
            DaySummaries.Clear();
            SelectedPreferences.Clear();
            PreferenceSummary = "Sin materias registradas";
            SelectedSchedule = null;
            ShowSaveSheetPrompt = false;
            SheetNameToSave = string.Empty;
            _contactSyncOffered = false;

            var sheetId = GoogleSheetId.Trim();
            var names = await Task.Run(() => _sheetReader.ListSheetNames(sheetId));
            foreach (var name in names)
            {
                SheetNames.Add(name);
            }

            if (SheetNames.Count == 1)
            {
                SelectedSheet = SheetNames[0];
            }

            Status = SheetNames.Count > 0 ? "Seleccione hoja" : "Sin hojas detectadas";
            await OfferSavePromptIfNeededAsync();
        }
        catch (Exception ex)
        {
            Status = "Error al cargar Google Sheets";
            Errors.Clear();
            Errors.Add(ex.Message);
            Logger.LogError(nameof(LoadGoogleSheetsAsync), ex);
        }
    }

    private bool CanLoadGoogleSheets() => !string.IsNullOrWhiteSpace(GoogleSheetId);
    private sealed class SheetHistoryExport
    {
        public List<SheetIdEntry> SheetIds { get; set; } = new();
    }

    private async Task SaveAliasAsync()
    {
        if (SelectedSheetHistory is null || string.IsNullOrWhiteSpace(SheetAliasEdit))
        {
            return;
        }

        var entry = new SheetIdEntry
        {
            Name = SheetAliasEdit.Trim(),
            SheetId = SelectedSheetHistory.SheetId,
            CreatedAtUtc = string.IsNullOrWhiteSpace(SelectedSheetHistory.CreatedAtUtc)
                ? DateTime.UtcNow.ToString("O")
                : SelectedSheetHistory.CreatedAtUtc
        };

        await _sheetHistoryRepository.UpsertAsync(entry);
        await LoadSheetHistoryAsync();
        SelectedSheetHistory = SheetHistory.FirstOrDefault(x => x.SheetId == entry.SheetId);
    }

    private bool CanSaveAlias()
        => SelectedSheetHistory is not null && !string.IsNullOrWhiteSpace(SheetAliasEdit);

    private async Task ExportSheetIdsAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON (*.json)|*.json|Todos los archivos|*.*",
            FileName = "sheet-ids.json"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var export = new SheetHistoryExport
        {
            SheetIds = SheetHistory.Select(x => new SheetIdEntry
            {
                Name = x.Name,
                SheetId = x.SheetId,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList()
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(dialog.FileName, json);
    }

    private async Task ImportSheetIdsAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON (*.json)|*.json|Todos los archivos|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var json = await File.ReadAllTextAsync(dialog.FileName);
        var export = JsonSerializer.Deserialize<SheetHistoryExport>(json);
        var list = export?.SheetIds;
        if (list is null)
        {
            list = JsonSerializer.Deserialize<List<SheetIdEntry>>(json) ?? new List<SheetIdEntry>();
        }

        list = list.Where(x => !string.IsNullOrWhiteSpace(x.SheetId) && !string.IsNullOrWhiteSpace(x.Name)).ToList();
        if (list.Count == 0)
        {
            return;
        }

        var choice = MessageBox.Show(
            "¿Reemplazar el historial actual?\n\nSi = Reemplazar\nNo = Combinar\nCancelar = No hacer cambios",
            "Importar IDs",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (choice == MessageBoxResult.Cancel)
        {
            return;
        }

        if (choice == MessageBoxResult.Yes)
        {
            var existing = await _sheetHistoryRepository.GetAllAsync();
            foreach (var item in existing)
            {
                await _sheetHistoryRepository.DeleteAsync(item.Id);
            }
        }

        foreach (var entry in list)
        {
            entry.CreatedAtUtc = string.IsNullOrWhiteSpace(entry.CreatedAtUtc)
                ? DateTime.UtcNow.ToString("O")
                : entry.CreatedAtUtc;
            await _sheetHistoryRepository.UpsertAsync(entry);
        }

        await LoadSheetHistoryAsync();
    }
    private async Task LoadSheetHistoryAsync()
    {
        try
        {
            SheetHistory.Clear();
            var items = await _sheetHistoryRepository.GetAllAsync();
            foreach (var item in items)
            {
                SheetHistory.Add(item);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(LoadSheetHistoryAsync), ex);
        }
    }

    private async Task UseSelectedSheetIdAsync()
    {
        if (SelectedSheetHistory is null) return;
        GoogleSheetId = SelectedSheetHistory.SheetId;
        SelectedSource = SourceSheets;
        await LoadGoogleSheetsAsync();
    }

    private async Task DeleteSheetIdByItemAsync(SheetIdEntry? entry)
    {
        if (entry is null) return;
        await _sheetHistoryRepository.DeleteAsync(entry.Id);
        await LoadSheetHistoryAsync();
    }

    private async Task DeleteSelectedSheetIdAsync()
    {
        if (SelectedSheetHistory is null) return;
        await _sheetHistoryRepository.DeleteAsync(SelectedSheetHistory.Id);
        await LoadSheetHistoryAsync();
    }

    private async Task SaveSheetIdAsync()
    {
        if (string.IsNullOrWhiteSpace(GoogleSheetId) || string.IsNullOrWhiteSpace(SheetNameToSave))
        {
            return;
        }

        var entry = new SheetIdEntry
        {
            Name = SheetNameToSave.Trim(),
            SheetId = GoogleSheetId.Trim(),
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        };

        await _sheetHistoryRepository.UpsertAsync(entry);
        await LoadSheetHistoryAsync();
        ShowSaveSheetPrompt = false;
        SheetNameToSave = string.Empty;
    }

    private bool CanSaveSheetId() => !string.IsNullOrWhiteSpace(GoogleSheetId) && !string.IsNullOrWhiteSpace(SheetNameToSave);

    private void CancelSaveSheetPrompt()
    {
        ShowSaveSheetPrompt = false;
        SheetNameToSave = string.Empty;
    }

    private async Task OfferSavePromptIfNeededAsync()
    {
        if (string.IsNullOrWhiteSpace(GoogleSheetId)) return;
        var existing = await _sheetHistoryRepository.GetBySheetIdAsync(GoogleSheetId.Trim());
        if (existing is null)
        {
            ShowSaveSheetPrompt = true;
        }
    }
    public async Task AnalyzeAsync()
    {
        if (!CanAnalyze() || _isRefreshing) return;
        _isRefreshing = true;
        Status = "Analizando horarios...";
        Errors.Clear();

        try
        {
            var reader = IsGoogleSource ? _sheetReader : _fileReader;
            var source = IsGoogleSource ? GoogleSheetId : ExcelPath;
            if (string.IsNullOrWhiteSpace(source))
            {
                Status = "Fuente no valida";
                return;
            }
            var result = await Task.Run(() => reader.Read(source!, SelectedSheet!));
            Schedules.Clear();
            foreach (var schedule in result.Schedules)
            {
                Schedules.Add(schedule);
            }

            Errors.Clear();
            foreach (var error in result.Errors)
            {
                Errors.Add(error);
            }

            Status = result.HasErrors
                ? "Terminado con avisos"
                : $"Docentes: {Schedules.Count}";
            LastRefreshText = DateTime.Now.ToString("g");
            UpdateVisibleTeachers();
            await TryOfferContactSyncAsync();
        }
        catch (Exception ex)
        {
            Status = "Error al analizar horarios";
            Errors.Clear();
            Errors.Add(ex.Message);
            Logger.LogError(nameof(AnalyzeAsync), ex);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private bool CanAnalyze()
    {
        if (IsGoogleSource)
        {
            return !string.IsNullOrWhiteSpace(GoogleSheetId) && !string.IsNullOrWhiteSpace(SelectedSheet);
        }

        return !string.IsNullOrWhiteSpace(ExcelPath) && !string.IsNullOrWhiteSpace(SelectedSheet);
    }

    private void ConfigureTimer()
    {
        if (!AutoRefreshEnabled)
        {
            _timer?.Stop();
            return;
        }

        _timer ??= new DispatcherTimer();
        _timer.Stop();
        _timer.Interval = TimeSpan.FromMinutes(SelectedRefreshInterval);
        _timer.Tick -= OnTimerTick;
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        if (!AutoRefreshEnabled)
        {
            return;
        }

        await AnalyzeAsync();
    }

    private void RefreshDaySummaries()
    {
        DaySummaries.Clear();
        if (SelectedSchedule is null)
        {
            return;
        }

        foreach (var day in DayOrder)
        {
            if (!SelectedSchedule.BloquesPorDia.TryGetValue(day, out var blocks))
            {
                continue;
            }

            var ranges = blocks.Select(b => $"{b.Start:hh\\:mm}-{b.End:hh\\:mm}").ToList();
            var rangesText = string.Join(" y ", ranges);
            SelectedSchedule.HorasPorDia.TryGetValue(day, out var hours);

            DaySummaries.Add(new ScheduleDaySummary
            {
                Day = day,
                RangesText = rangesText,
                Hours = hours
            });
        }
    }

    private void RefreshPreferences()
    {
        SelectedPreferences.Clear();
        if (SelectedSchedule is null || SelectedSchedule.SubjectPreferences.Count == 0)
        {
            PreferenceSummary = "Sin materias registradas";
            return;
        }

        var ordered = SelectedSchedule.SubjectPreferences
            .OrderByDescending(p => GetPriorityWeight(p.Priority))
            .ThenBy(p => p.Subject, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        foreach (var pref in ordered)
        {
            SelectedPreferences.Add(pref);
        }

        PreferenceSummary = $"Materias preferidas: {SelectedPreferences.Count}";
    }

    private static int GetPriorityWeight(string priority) => priority switch
    {
        "Alta" => 3,
        "Media" => 2,
        "Baja" => 1,
        _ => 0
    };

    private async Task TryOfferContactSyncAsync()
    {
        if (_contactSyncOffered || !IsGoogleSource)
        {
            return;
        }

        if (_sheetReader is not ScheduleSheetsReader sheetsReader)
        {
            _contactSyncOffered = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(GoogleSheetId) || string.IsNullOrWhiteSpace(SelectedSheet))
        {
            return;
        }

        var contacts = sheetsReader.ReadContacts(GoogleSheetId.Trim(), SelectedSheet);
        _contactSyncOffered = true;
        if (contacts.Count == 0)
        {
            return;
        }

        var answer = MessageBox.Show(
            "Se detectaron contactos en la hoja. ¿Deseas actualizar el directorio con esos datos?",
            "Actualizar directorio",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        await SyncContactsAsync(contacts);
    }

    private async Task SyncContactsAsync(List<ContactImportRow> imported)
    {
        var existing = (await _directoryRepository.GetAllAsync()).ToList();
        var usedNames = new HashSet<string>(existing.Select(c => c.NombreNormalizado));

        foreach (var row in imported)
        {
            var displayName = SanitizeDisplayName(row.Name);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            var normalized = NameNormalizer.Normalize(displayName);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            var bestMatch = FindBestMatch(existing, normalized);
            var email = row.Email.Trim();
            if (bestMatch.Match is not null && bestMatch.Score >= 0.8)
            {
                if (string.Equals(bestMatch.Match.Correo, email, StringComparison.OrdinalIgnoreCase))
                {
                    var phone = NormalizePhone(row.Phone);
                    if (string.IsNullOrWhiteSpace(bestMatch.Match.Telefono) && !string.IsNullOrWhiteSpace(phone))
                    {
                        bestMatch.Match.Telefono = phone;
                        bestMatch.Match.LastUpdatedUtc = DateTime.UtcNow.ToString("O");
                        await _directoryRepository.UpsertAsync(bestMatch.Match);
                    }
                    continue;
                }

                var aliasName = BuildAliasName(bestMatch.Match.NombreVisible, existing);
                var aliasNormalized = NameNormalizer.Normalize(aliasName);
                var newContact = new Contact
                {
                    NombreVisible = aliasName,
                    NombreNormalizado = aliasNormalized,
                    Correo = email,
                    Telefono = NormalizePhone(row.Phone),
                    Activo = true,
                    LastUpdatedUtc = DateTime.UtcNow.ToString("O")
                };

                await _directoryRepository.UpsertAsync(newContact);
                existing.Add(newContact);
                usedNames.Add(aliasNormalized);
                continue;
            }

            if (existing.Any(c => string.Equals(c.Correo, email, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var newEntry = new Contact
            {
                NombreVisible = displayName,
                NombreNormalizado = normalized,
                Correo = email,
                Telefono = NormalizePhone(row.Phone),
                Activo = true,
                LastUpdatedUtc = DateTime.UtcNow.ToString("O")
            };

            await _directoryRepository.UpsertAsync(newEntry);
            existing.Add(newEntry);
            usedNames.Add(normalized);
        }
    }

    private static (Contact? Match, double Score) FindBestMatch(List<Contact> existing, string normalized)
    {
        Contact? best = null;
        var bestScore = 0.0;
        foreach (var contact in existing)
        {
            var score = CalculateSimilarity(normalized, contact.NombreNormalizado);
            if (score > bestScore)
            {
                best = contact;
                bestScore = score;
            }
        }

        return (best, bestScore);
    }

    private static double CalculateSimilarity(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return 0;
        }

        var distance = StringSimilarity.LevenshteinDistance(a, b);
        var maxLen = Math.Max(a.Length, b.Length);
        return maxLen == 0 ? 0 : 1.0 - (double)distance / maxLen;
    }

    private static string SanitizeDisplayName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var filtered = new string(input.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());
        filtered = System.Text.RegularExpressions.Regex.Replace(filtered, @"\s+", " ").Trim();
        if (string.IsNullOrWhiteSpace(filtered))
        {
            return string.Empty;
        }

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(filtered.ToLower());
    }

    private static string BuildAliasName(string baseName, List<Contact> existing)
    {
        var root = string.IsNullOrWhiteSpace(baseName) ? "Contacto" : baseName.Trim();
        var maxSuffix = 1;
        foreach (var contact in existing)
        {
            if (string.Equals(contact.NombreVisible, root, StringComparison.OrdinalIgnoreCase))
            {
                maxSuffix = Math.Max(maxSuffix, 1);
                continue;
            }

            if (contact.NombreVisible.StartsWith(root + " (", StringComparison.OrdinalIgnoreCase))
            {
                var start = root.Length + 2;
                var end = contact.NombreVisible.IndexOf(')', start);
                if (end > start && int.TryParse(contact.NombreVisible.Substring(start, end - start), out var parsed))
                {
                    maxSuffix = Math.Max(maxSuffix, parsed);
                }
            }
        }

        var count = maxSuffix + 1;
        return $"{root} ({count})";
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length == 10 ? digits : null;
    }    private void UpdateVisibleTeachers()
    {
        var items = SchedulesView.Cast<TeacherSchedule>().ToList();
        VisibleTeachers = items.Count;
        AverageHours = VisibleTeachers > 0 ? items.Average(s => s.TotalSemanalHoras) : 0;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}









































