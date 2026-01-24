using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AppParaUniversidad.Common;
using AppParaUniversidad.Domain.Models;
using AppParaUniversidad.Services.Directory;

namespace AppParaUniversidad.ViewModels;

public class DirectoryViewModel : INotifyPropertyChanged
{
    private readonly IDirectoryRepository _repository;
    private Contact? _selected;
    private string _nombreVisible = string.Empty;
    private string _correo = string.Empty;
    private string? _telefono;
    private bool _activo = true;
    private string _status = "Listo";
    private string _lastUpdated = string.Empty;

    public ObservableCollection<Contact> Contacts { get; } = new();

    public bool SelectAll
    {
        get => Contacts.Count > 0 && Contacts.All(c => c.SelectedForDelete);
        set
        {
            foreach (var contact in Contacts)
            {
                contact.SelectedForDelete = value;
            }
            OnPropertyChanged();
        }
    }

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

    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand DeleteCommand { get; }

    public DirectoryViewModel(IDirectoryRepository repository)
    {
        _repository = repository;
        _repository.InitializeAsync().GetAwaiter().GetResult();

        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        NewCommand = new RelayCommand(_ => ClearForm());
        DeleteCommand = new AsyncRelayCommand(DeleteAsync);

        _ = LoadAsync();
    }

    public Contact? Selected
    {
        get => _selected;
        set
        {
            if (_selected != value)
            {
                _selected = value;
                OnPropertyChanged();
                CopySelectedToForm();
                RaiseCanExecute();
            }
        }
    }

    public string NombreVisible
    {
        get => _nombreVisible;
        set
        {
            if (_nombreVisible != value)
            {
                _nombreVisible = value;
                OnPropertyChanged();
                RaiseCanExecute();
            }
        }
    }

    public string Correo
    {
        get => _correo;
        set
        {
            if (_correo != value)
            {
                _correo = value;
                OnPropertyChanged();
                RaiseCanExecute();
            }
        }
    }

    public string? Telefono
    {
        get => _telefono;
        set
        {
            if (_telefono != value)
            {
                _telefono = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Activo
    {
        get => _activo;
        set
        {
            if (_activo != value)
            {
                _activo = value;
                OnPropertyChanged();
            }
        }
    }

    public string LastUpdated
    {
        get => _lastUpdated;
        set
        {
            if (_lastUpdated != value)
            {
                _lastUpdated = value;
                OnPropertyChanged();
            }
        }
    }

    private void CopySelectedToForm()
    {
        if (Selected is null) return;
        NombreVisible = Selected.NombreVisible;
        Correo = Selected.Correo;
        Telefono = Selected.Telefono;
        Activo = Selected.Activo;
        LastUpdated = Selected.LastUpdatedUtc;
    }

    private void ClearForm()
    {
        Selected = null;
        NombreVisible = string.Empty;
        Correo = string.Empty;
        Telefono = null;
        Activo = true;
        LastUpdated = string.Empty;
        RaiseCanExecute();
    }

        private void RegisterContact(Contact contact)
    {
        contact.PropertyChanged += OnContactPropertyChanged;
    }

    private void OnContactPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Contact.SelectedForDelete))
        {
            OnPropertyChanged(nameof(SelectAll));
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            Contacts.Clear();
            var contacts = await _repository.GetAllAsync();
            foreach (var contact in contacts)
            {
                RegisterContact(contact);
                Contacts.Add(contact);
            }
            Status = $"Contactos: {Contacts.Count}";
            OnPropertyChanged(nameof(SelectAll));
            RaiseCanExecute();
        }
        catch (Exception ex)
        {
            AppParaUniversidad.Common.Logger.LogError(nameof(LoadAsync), ex);
            Status = "Error al cargar directorio";
        }
    }

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(NombreVisible) && !string.IsNullOrWhiteSpace(Correo);

    private async Task SaveAsync()
    {
        try
        {
            var normalized = NameNormalizer.Normalize(NombreVisible);
            var contact = new Contact
            {
                NombreVisible = NombreVisible.Trim(),
                NombreNormalizado = normalized,
                Correo = Correo.Trim(),
                Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim(),
                Activo = Activo,
                LastUpdatedUtc = DateTime.UtcNow.ToString("O")
            };

            await _repository.UpsertAsync(contact);
            Status = "Guardado";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            AppParaUniversidad.Common.Logger.LogError(nameof(SaveAsync), ex);
            Status = "Error al guardar";
        }
    }

    private async Task DeleteAsync()
    {
        try
        {
            var selected = Contacts.Where(c => c.SelectedForDelete).ToList();
            if (selected.Count == 0)
            {
                Status = "Selecciona contactos para eliminar";
                return;
            }

            foreach (var contact in selected)
            {
                await _repository.DeleteAsync(contact.NombreNormalizado);
            }

            ClearForm();
            Status = "Eliminados";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            AppParaUniversidad.Common.Logger.LogError(nameof(DeleteAsync), ex);
            Status = "Error al eliminar";
        }
    }

    private void RaiseCanExecute()
    {
        if (SaveCommand is AsyncRelayCommand asc) asc.RaiseCanExecuteChanged();
        if (DeleteCommand is AsyncRelayCommand adc) adc.RaiseCanExecuteChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}





