using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AppParaUniversidad.Common;
using AppParaUniversidad.Services.Settings;
using AppParaUniversidad.Services.Updates;

namespace AppParaUniversidad.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly AppSettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly Action<double, double> _applyWindowSize;
    private readonly SendViewModel _sendViewModel;
    private readonly GitHubUpdateService _updateService = new();

    private bool _darkTheme;
    private WindowSizePreset? _selectedWindowSize;
    private string _updateStatusText = "Verificando...";
    private bool _updateAvailable;
    private bool _updateCheckSucceeded;
    private bool _isUpdating;
    private GitHubUpdateInfo? _latestRelease;

    public ICommand ApplySizeCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand LoadCredentialCommand => _sendViewModel.LoadCredentialCommand;
    public ICommand CheckUpdatesCommand { get; }

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

    public string UpdateStatusText
    {
        get => _updateStatusText;
        private set
        {
            if (_updateStatusText != value)
            {
                _updateStatusText = value;
                OnPropertyChanged();
            }
        }
    }

    public bool UpdateAvailable
    {
        get => _updateAvailable;
        private set
        {
            if (_updateAvailable != value)
            {
                _updateAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UpdateButtonText));
                OnPropertyChanged(nameof(UpdateButtonEnabled));
            }
        }
    }

    public bool UpdateCheckSucceeded
    {
        get => _updateCheckSucceeded;
        private set
        {
            if (_updateCheckSucceeded != value)
            {
                _updateCheckSucceeded = value;
                OnPropertyChanged();
            }
        }
    }

    public string UpdateButtonText => UpdateAvailable ? "Actualizar ahora" : "Buscar actualizaciones";

    public bool UpdateButtonEnabled => !_isUpdating;

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
        CheckUpdatesCommand = new AsyncRelayCommand(ExecuteUpdateActionAsync, () => !_isUpdating);

        if (UpdateCheckState.HasChecked)
        {
            UpdateCheckSucceeded = UpdateCheckState.CheckSucceeded;
            UpdateAvailable = UpdateCheckState.UpdateAvailable;
            UpdateStatusText = UpdateCheckState.StatusText;
            _latestRelease = UpdateCheckState.Release;
        }
        else
        {
            _ = CheckForUpdatesAsync();
        }
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

    private async Task ExecuteUpdateActionAsync()
    {
        if (_isUpdating)
        {
            return;
        }

        if (UpdateAvailable && _latestRelease is not null)
        {
            await ApplyUpdateFromReleaseAsync(_latestRelease);
            return;
        }

        await CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        SetUpdating(true);

        try
        {
            var currentVersion = typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
            var result = await _updateService.CheckForUpdatesAsync(currentVersion);
            ApplyUpdateState(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(CheckForUpdatesAsync), ex);
            ApplyUpdateState(new UpdateCheckResult
            {
                IsSuccessful = false,
                StatusText = "No se pudo verificar"
            });
        }
        finally
        {
            SetUpdating(false);
        }
    }

    private async Task ApplyUpdateFromReleaseAsync(GitHubUpdateInfo release)
    {
        SetUpdating(true);

        try
        {
            UpdateStatusText = "Descargando actualizacion...";
            var result = await _updateService.ApplyUpdateAsync(release);

            if (result.RequiresShutdown)
            {
                Application.Current.Shutdown();
                return;
            }

            if (result.OpenReleasePage)
            {
                if (!string.IsNullOrWhiteSpace(release.HtmlUrl))
                {
                    OpenUrl(release.HtmlUrl);
                }

                MessageBox.Show(result.Message, "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
                ApplyUpdateState(new UpdateCheckResult
                {
                    IsSuccessful = true,
                    UpdateAvailable = true,
                    StatusText = $"Actualizacion disponible ({GitHubUpdateService.NormalizeTag(release.TagName)})",
                    Release = release
                });
                return;
            }

            if (!result.Started)
            {
                MessageBox.Show(result.Message, "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
                ApplyUpdateState(new UpdateCheckResult
                {
                    IsSuccessful = true,
                    UpdateAvailable = true,
                    StatusText = $"Actualizacion disponible ({GitHubUpdateService.NormalizeTag(release.TagName)})",
                    Release = release
                });
                return;
            }

            MessageBox.Show(result.Message, "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
            ApplyUpdateState(new UpdateCheckResult
            {
                IsSuccessful = true,
                UpdateAvailable = true,
                StatusText = $"Actualizacion disponible ({GitHubUpdateService.NormalizeTag(release.TagName)})",
                Release = release
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(ApplyUpdateFromReleaseAsync), ex);
            MessageBox.Show("No se pudo aplicar la actualizacion automaticamente. Revisa logs para mas detalle.", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Error);
            ApplyUpdateState(new UpdateCheckResult
            {
                IsSuccessful = false,
                StatusText = "No se pudo verificar"
            });
        }
        finally
        {
            SetUpdating(false);
        }
    }

    private void SetUpdating(bool value)
    {
        _isUpdating = value;
        OnPropertyChanged(nameof(UpdateButtonEnabled));
        if (CheckUpdatesCommand is AsyncRelayCommand cmd)
        {
            cmd.RaiseCanExecuteChanged();
        }
    }

    private void ApplyUpdateState(UpdateCheckResult result)
    {
        UpdateCheckSucceeded = result.IsSuccessful;
        UpdateAvailable = result.UpdateAvailable;
        UpdateStatusText = result.StatusText;
        _latestRelease = result.Release;

        UpdateCheckState.HasChecked = true;
        UpdateCheckState.CheckSucceeded = result.IsSuccessful;
        UpdateCheckState.UpdateAvailable = result.UpdateAvailable;
        UpdateCheckState.StatusText = result.StatusText;
        UpdateCheckState.Release = result.Release;
    }

    private static void OpenUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
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
