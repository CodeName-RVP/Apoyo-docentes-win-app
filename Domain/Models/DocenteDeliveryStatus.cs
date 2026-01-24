using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppParaUniversidad.Domain.Models;

public class DocenteDeliveryStatus : INotifyPropertyChanged
{
    private string _correo = string.Empty;
    private bool _tieneCorreo;
    private bool _enviar;
    private IReadOnlyList<string> _sugerencias = new List<string>();
    private IReadOnlyList<string> _correosDisponibles = new List<string>();

    public string NombreVisible { get; set; } = string.Empty;
    public string NombreNormalizado { get; set; } = string.Empty;

    public string? Correo
    {
        get => _correo;
        set
        {
            if (_correo != value)
            {
                _correo = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    public bool TieneCorreo
    {
        get => _tieneCorreo;
        set
        {
            if (_tieneCorreo != value)
            {
                _tieneCorreo = value;
                OnPropertyChanged();
            }
        }
    }

    public IReadOnlyList<string> Sugerencias
    {
        get => _sugerencias;
        set
        {
            if (_sugerencias != value)
            {
                _sugerencias = value ?? new List<string>();
                OnPropertyChanged();
            }
        }
    }

    public IReadOnlyList<string> CorreosDisponibles
    {
        get => _correosDisponibles;
        set
        {
            if (_correosDisponibles != value)
            {
                _correosDisponibles = value ?? new List<string>();
                OnPropertyChanged();
            }
        }
    }

    // Selección para envío
    public bool Enviar
    {
        get => _enviar;
        set
        {
            if (_enviar != value)
            {
                _enviar = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
