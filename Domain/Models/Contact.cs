using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppParaUniversidad.Domain.Models;

public class Contact : INotifyPropertyChanged
{
    public Contact()
    {
    }

    public Contact(string nombreVisible, string nombreNormalizado, string correo, string? telefono, bool activo, string? lastUpdatedUtc = null)
    {
        NombreVisible = nombreVisible;
        NombreNormalizado = nombreNormalizado;
        Correo = correo;
        Telefono = telefono;
        Activo = activo;
        LastUpdatedUtc = lastUpdatedUtc ?? string.Empty;
    }

    public string NombreVisible { get; set; } = string.Empty;
    public string NombreNormalizado { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
    public string LastUpdatedUtc { get; set; } = string.Empty;

    public string LastUpdatedDisplay
    {
        get
        {
            if (DateTime.TryParse(
                LastUpdatedUtc, 
                null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var date))
            {
                return date.ToLocalTime().ToString("dd/MM/yyyy");
            }
            return string.Empty;
        }
    }
    private bool _selectedForDelete;

    public bool SelectedForDelete
    {
        get => _selectedForDelete;
        set
        {
            if (_selectedForDelete != value)
            {
                _selectedForDelete = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}


