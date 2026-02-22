using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
            UpdateAvailable = UpdateCheckState.UpdateAvailable;
            UpdateStatusText = UpdateCheckState.StatusText;
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

        if (UpdateAvailable)
        {
            await DownloadAndApplyUpdateAsync();
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
            _latestRelease = await _updateService.GetLatestReleaseAsync();
            if (_latestRelease is null || string.IsNullOrWhiteSpace(_latestRelease.TagName))
            {
                SetUpdateState(true, "Actualizacion disponible");
                return;
            }

            var hasUpdate = GitHubUpdateService.IsRemoteNewer(currentVersion, _latestRelease.TagName);
            SetUpdateState(hasUpdate, hasUpdate ? "Actualizacion disponible" : "Actualizado");
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(CheckForUpdatesAsync), ex);
            SetUpdateState(true, "Actualizacion disponible");
        }
        finally
        {
            SetUpdating(false);
        }
    }

    private async Task DownloadAndApplyUpdateAsync()
    {
        SetUpdating(true);

        try
        {
            var release = _latestRelease ?? await _updateService.GetLatestReleaseAsync();
            if (release is null)
            {
                MessageBox.Show("No se pudo obtener la informacion de la actualizacion.", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(release.AssetDownloadUrl))
            {
                MessageBox.Show("No hay archivo de actualizacion en el release. Se abrira GitHub para actualizar manualmente.", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
                OpenUrl(release.HtmlUrl);
                return;
            }

            UpdateStatusText = "Descargando actualizacion...";

            var tempRoot = Path.Combine(Path.GetTempPath(), "ApoyoDocentesUpdater", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            var assetName = string.IsNullOrWhiteSpace(release.AssetName) ? "update.bin" : release.AssetName;
            var assetPath = Path.Combine(tempRoot, assetName);

            await _updateService.DownloadFileAsync(release.AssetDownloadUrl, assetPath);

            var extension = Path.GetExtension(assetPath).ToLowerInvariant();
            if (extension == ".msi" || extension == ".exe")
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = assetPath,
                    UseShellExecute = true
                });

                UpdateStatusText = "Instalador descargado";
                return;
            }

            if (extension != ".zip")
            {
                MessageBox.Show("Formato de actualizacion no soportado automaticamente. Se abrira GitHub.", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
                OpenUrl(release.HtmlUrl);
                return;
            }

            var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(currentExe))
            {
                MessageBox.Show("No se pudo detectar el ejecutable actual.", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var appDir = Path.GetDirectoryName(currentExe);
            if (string.IsNullOrWhiteSpace(appDir))
            {
                MessageBox.Show("No se pudo detectar la carpeta de la aplicacion.", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var exeName = Path.GetFileName(currentExe);
            var extractDir = Path.Combine(tempRoot, "extract");
            ZipFile.ExtractToDirectory(assetPath, extractDir, true);

            var updaterScript = Path.Combine(tempRoot, "apply-update.cmd");
            var script = $"@echo off{Environment.NewLine}" +
                         $"ping 127.0.0.1 -n 3 > nul{Environment.NewLine}" +
                         $"robocopy \"{extractDir}\" \"{appDir}\" /E /R:2 /W:1 > nul{Environment.NewLine}" +
                         $"start \"\" \"{Path.Combine(appDir, exeName)}\"{Environment.NewLine}";

            File.WriteAllText(updaterScript, script);

            Process.Start(new ProcessStartInfo
            {
                FileName = updaterScript,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            });

            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(DownloadAndApplyUpdateAsync), ex);
            MessageBox.Show("No se pudo aplicar la actualizacion automaticamente. Revisa logs para mas detalle.", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Error);
            SetUpdateState(true, "Actualizacion disponible");
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

    private void SetUpdateState(bool available, string status)
    {
        UpdateAvailable = available;
        UpdateStatusText = status;
        UpdateCheckState.HasChecked = true;
        UpdateCheckState.UpdateAvailable = available;
        UpdateCheckState.StatusText = status;
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
