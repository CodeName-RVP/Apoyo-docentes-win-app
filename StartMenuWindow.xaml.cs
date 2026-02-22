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
            var latest = await _updateService.GetLatestReleaseAsync();
            if (latest is null || string.IsNullOrWhiteSpace(latest.TagName))
            {
                UpdateCheckState.HasChecked = true;
                UpdateCheckState.UpdateAvailable = true;
                UpdateCheckState.StatusText = "Actualizacion disponible";
                return;
            }

            var hasUpdate = GitHubUpdateService.IsRemoteNewer(currentVersion, latest.TagName);
            UpdateCheckState.HasChecked = true;
            UpdateCheckState.UpdateAvailable = hasUpdate;
            UpdateCheckState.StatusText = hasUpdate ? "Actualizacion disponible" : "Actualizado";
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(CheckUpdatesAtStartupAsync), ex);
            UpdateCheckState.HasChecked = true;
            UpdateCheckState.UpdateAvailable = true;
            UpdateCheckState.StatusText = "Actualizacion disponible";
        }
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
