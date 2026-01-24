using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AppParaUniversidad.Common;
using AppParaUniversidad.Services.Settings;

namespace AppParaUniversidad.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly AppSettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly Action<double, double> _applyWindowSize;
    private readonly SendViewModel _sendViewModel;

    private bool _darkTheme;
    private WindowSizePreset? _selectedWindowSize;

    public ICommand ApplySizeCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand LoadCredentialCommand => _sendViewModel.LoadCredentialCommand;

    public bool GmailReady => _sendViewModel.GmailReady;
    public string GmailStatus => _sendViewModel.GmailStatus;
    public string GmailButtonText => _sendViewModel.GmailButtonText;

    public ObservableCollection<WindowSizePreset> WindowSizePresets { get; }

    public WindowSizePreset? SelectedWindowSize
    {
        get => _selectedWindowSize;
        set
        {
            if (!ReferenceEquals(_selectedWindowSize, value))
            {
                _selectedWindowSize = value;
                OnPropertyChanged();
            }
        }
    }

    public bool DarkTheme
    {
        get => _darkTheme;
        set
        {
            if (_darkTheme != value)
            {
                _darkTheme = value;
                OnPropertyChanged();
            }
        }
    }

    public SettingsViewModel(AppSettingsService settingsService, Action<double, double> applyWindowSize, SendViewModel sendViewModel)
    {
        _settingsService = settingsService;
        _applyWindowSize = applyWindowSize;
        _sendViewModel = sendViewModel;
        _settings = _settingsService.Load();

        _darkTheme = _settings.DarkTheme;

        WindowSizePresets = new ObservableCollection<WindowSizePreset>
        {
            new WindowSizePreset("Compacto (1000 x 700)", 1000, 700),
            new WindowSizePreset("Medio (1200 x 780)", 1200, 780),
            new WindowSizePreset("Amplio (1400 x 900)", 1400, 900),
            new WindowSizePreset("Extra (1600 x 1000)", 1600, 1000),
        };
        SelectedWindowSize = FindPreset(_settings.WindowWidth, _settings.WindowHeight) ?? WindowSizePresets[1];

        _sendViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SendViewModel.GmailReady))
            {
                OnPropertyChanged(nameof(GmailReady));
            }
            else if (e.PropertyName == nameof(SendViewModel.GmailStatus))
            {
                OnPropertyChanged(nameof(GmailStatus));
            }
            else if (e.PropertyName == nameof(SendViewModel.GmailButtonText))
            {
                OnPropertyChanged(nameof(GmailButtonText));
            }
        };

        ApplySizeCommand = new RelayCommand(_ => ApplySize());
        ToggleThemeCommand = new RelayCommand(_ => ApplyTheme());
    }

    private void ApplySize()
    {
        if (SelectedWindowSize is null)
        {
            return;
        }

        _settings.WindowWidth = SelectedWindowSize.Width;
        _settings.WindowHeight = SelectedWindowSize.Height;
        _settingsService.Save(_settings);
        _applyWindowSize(SelectedWindowSize.Width, SelectedWindowSize.Height);
    }

    private void ApplyTheme()
    {
        _settings.DarkTheme = DarkTheme;
        _settingsService.Save(_settings);
        ThemeManager.ApplyTheme(DarkTheme);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private WindowSizePreset? FindPreset(double width, double height)
    {
        foreach (var preset in WindowSizePresets)
        {
            if (Math.Abs(preset.Width - width) < 0.5 && Math.Abs(preset.Height - height) < 0.5)
            {
                return preset;
            }
        }

        return null;
    }
}

public sealed class WindowSizePreset
{
    public WindowSizePreset(string label, double width, double height)
    {
        Label = label;
        Width = width;
        Height = height;
    }

    public string Label { get; }
    public double Width { get; }
    public double Height { get; }

    public override string ToString() => Label;
}
