using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AppParaUniversidad.Common;
using AppParaUniversidad.Domain.Models;
using AppParaUniversidad.Services.Excel;
using AppParaUniversidad.Services.Loads;
using Microsoft.Win32;

namespace AppParaUniversidad.ViewModels;

public class LoadsViewModel : INotifyPropertyChanged
{
    private readonly IExcelReader _excelReader;
    private readonly AsyncRelayCommand _analyzeCommand;
    private readonly ITeachingLoadBuilder _teachingLoadBuilder;
    private string? _selectedSheet;
    private string? _excelPath;
    private string _status = "Seleccione archivo";

    public ObservableCollection<string> SheetNames { get; } = new();
    public ObservableCollection<TeachingAssignment> CurrentRows { get; } = new();
    public ObservableCollection<DocenteCarga> GroupedLoads { get; } = new();
    public ObservableCollection<string> Warnings { get; } = new();

    public ICommand OpenFileCommand { get; }
    public ICommand AnalyzeCommand => _analyzeCommand;

    public event Action<IReadOnlyList<DocenteCarga>>? GroupedUpdated;

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

    public LoadsViewModel(IExcelReader excelReader, ITeachingLoadBuilder teachingLoadBuilder)
    {
        _excelReader = excelReader;
        _teachingLoadBuilder = teachingLoadBuilder;
        OpenFileCommand = new RelayCommand(_ => OpenFile());
        _analyzeCommand = new AsyncRelayCommand(AnalyzeAsync, CanAnalyze);
    }

    private void OpenFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Todos los archivos|*.*",
            Multiselect = false
        };
        if (dlg.ShowDialog() != true) return;

        _ = LoadFileAsync(dlg.FileName);
    }

    public async Task LoadFileAsync(string path)
    {
        try
        {
            ExcelPath = path;
            SheetNames.Clear();
            foreach (var name in await _excelReader.ListSheetNamesAsync(path))
            {
                SheetNames.Add(name);
            }
            Status = "Seleccione hoja general";
            Warnings.Clear();
            CurrentRows.Clear();
            GroupedLoads.Clear();
        }
        catch (Exception ex)
        {
            Status = "Error al cargar Excel";
            Warnings.Clear();
            Warnings.Add(ex.Message);
            Logger.LogError(nameof(LoadFileAsync), ex);
        }
    }

    public async Task AnalyzeAsync()
    {
        if (!CanAnalyze()) return;
        Status = "Analizando...";

        try
        {
            var result = await _excelReader.DetectAndReadAsync(ExcelPath!, SelectedSheet!);

            CurrentRows.Clear();
            foreach (var row in result.Rows) CurrentRows.Add(row);

            Warnings.Clear();
            foreach (var warning in result.Warnings) Warnings.Add(warning);

            Status = $"Encabezados en fila {result.HeaderRowIndex}. Filas: {result.Rows.Count}";
            RebuildGroups();
        }
        catch (Exception ex)
        {
            Status = "Error al analizar hoja";
            Warnings.Clear();
            Warnings.Add(ex.Message);
            Logger.LogError(nameof(AnalyzeAsync), ex);
        }
    }

    private bool CanAnalyze() => !string.IsNullOrWhiteSpace(ExcelPath) && !string.IsNullOrWhiteSpace(SelectedSheet);

    private void RebuildGroups()
    {
        GroupedLoads.Clear();
        var grouped = _teachingLoadBuilder.BuildByDocente(CurrentRows);
        foreach (var carga in grouped)
        {
            GroupedLoads.Add(carga);
        }
        GroupedUpdated?.Invoke(GroupedLoads.ToList());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
