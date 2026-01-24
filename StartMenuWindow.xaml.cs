using System.Windows;
using System.Windows.Input;
using AppParaUniversidad.Common;
using AppParaUniversidad.Services.Settings;

namespace AppParaUniversidad;

public partial class StartMenuWindow : Window
{
    private readonly AppSettingsService _settingsService;
    private readonly AppSettings _settings;

    public StartMenuWindow()
    {
        InitializeComponent();
        _settingsService = new AppSettingsService();
        _settings = _settingsService.Load();
        ThemeManager.ApplyTheme(_settings.DarkTheme);
        ApplyWindowSize(_settings.WindowWidth, _settings.WindowHeight);
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

