using System;
using System.Windows;
using System.Windows.Input;
using AppParaUniversidad.Common;
using AppParaUniversidad.Services.Settings;
using AppParaUniversidad.Services.Updates;

namespace AppParaUniversidad;

public partial class StartMenuWindow : Window
{
    private readonly AppSettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly GitHubUpdateService _updateService = new();

    public string AppVersionText { get; }

    public StartMenuWindow()
    {
        InitializeComponent();
        DataContext = this;

        var version = GetType().Assembly.GetName().Version;
        AppVersionText = $"Version {version?.ToString(3) ?? "1.0.0"}";

        _settingsService = new AppSettingsService();
        _settings = _settingsService.Load();
        ThemeManager.ApplyTheme(_settings.DarkTheme);
        ApplyWindowSize(_settings.WindowWidth, _settings.WindowHeight);
        _ = CheckUpdatesAtStartupAsync();
    }

    private async System.Threading.Tasks.Task CheckUpdatesAtStartupAsync()
    {
        try
        {
            var currentVersion = GetType().Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
            var result = await _updateService.CheckForUpdatesAsync(currentVersion);
            ApplyUpdateState(result);

            if (!result.UpdateAvailable || result.Release is null)
            {
                return;
            }

            var remoteTag = GitHubUpdateService.NormalizeTag(result.Release.TagName);
            var answer = await Dispatcher.InvokeAsync(() =>
                MessageBox.Show(
                    $"Se detecto una nueva version disponible: {remoteTag}.\n\nDeseas actualizar la app ahora?",
                    "Actualizacion disponible",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information));

            if (answer == MessageBoxResult.Yes)
            {
                await ApplyUpdateFromReleaseAsync(result.Release);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(CheckUpdatesAtStartupAsync), ex);
            ApplyUpdateState(new UpdateCheckResult
            {
                IsSuccessful = false,
                StatusText = "No se pudo verificar"
            });
        }
    }

    private async System.Threading.Tasks.Task ApplyUpdateFromReleaseAsync(GitHubUpdateInfo release)
    {
        try
        {
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
                return;
            }

            if (!result.Started)
            {
                MessageBox.Show(result.Message, "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(result.Message, "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(ApplyUpdateFromReleaseAsync), ex);
            MessageBox.Show("No se pudo aplicar la actualizacion automaticamente. Revisa logs para mas detalle.", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void ApplyUpdateState(UpdateCheckResult result)
    {
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

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private void ApplyWindowSize(double width, double height)
    {
        if (width > 600) Width = width;
        if (height > 400) Height = height;
    }

    private void OnOpenDeliveryClick(object sender, RoutedEventArgs e)
    {
        var window = new MainWindow
        {
            Owner = this
        };
        Hide();
        window.Show();
    }

    private void OnOpenTeacherSchedulesClick(object sender, RoutedEventArgs e)
    {
        var window = new TeacherSchedulesWindow
        {
            Owner = this
        };
        Hide();
        window.Show();
    }

    private void OnOptionsClick(object sender, RoutedEventArgs e)
    {
        var window = new OptionsWindow();
        window.ShowDialog();
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        DragMove();
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
}
