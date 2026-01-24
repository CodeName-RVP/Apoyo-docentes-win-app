using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AppParaUniversidad.Domain.Models;
using AppParaUniversidad.Services.Directory;
using AppParaUniversidad.Services.Export;
using Microsoft.Win32;

namespace AppParaUniversidad.ViewModels;

public class ImportExportViewModel : INotifyPropertyChanged
{
    private readonly IDirectoryRepository _directoryRepository;
    private readonly IJsonExportService _jsonExportService;
    private readonly SendViewModel _sendViewModel;
    private string _status = "Listo";

    public ObservableCollection<Contact> PreviewContacts { get; } = new();
    public ICommand ExportCommand { get; }
    public ICommand ImportMergeCommand { get; }

    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    public ImportExportViewModel(
        IDirectoryRepository directoryRepository,
        IJsonExportService jsonExportService,
        SendViewModel sendViewModel)
    {
        _directoryRepository = directoryRepository;
        _jsonExportService = jsonExportService;
        _sendViewModel = sendViewModel;

        ExportCommand = new AsyncRelayCommand(ExportAsync);
        ImportMergeCommand = new AsyncRelayCommand(ImportMergeAsync);
    }

    private async Task ExportAsync()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            FileName = "app-data.json"
        };
        if (dlg.ShowDialog() != true) return;

        var contacts = await _directoryRepository.GetAllAsync();
        var logs = _sendViewModel.Logs.ToList();
        await _jsonExportService.ExportAsync(dlg.FileName, contacts, logs);
        Status = $"Exportado: {dlg.FileName}";
    }

    private async Task ImportMergeAsync()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            Multiselect = false
        };
        if (dlg.ShowDialog() != true) return;

        var package = await _jsonExportService.ImportAsync(dlg.FileName);
        var added = 0;
        foreach (var contact in package.Contacts)
        {
            await _directoryRepository.UpsertAsync(contact);
            added++;
        }

        PreviewContacts.Clear();
        foreach (var c in package.Contacts)
        {
            PreviewContacts.Add(c);
        }

        Status = $"Importado (merge): {added} contactos";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
