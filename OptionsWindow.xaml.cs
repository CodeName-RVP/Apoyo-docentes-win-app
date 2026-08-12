using System.Windows;
using System.Windows.Input;
using AppParaUniversidad.Common;
using AppParaUniversidad.Services.Directory;
using AppParaUniversidad.Services.Email;
using AppParaUniversidad.Services.Mail;
using AppParaUniversidad.Services.Settings;
using AppParaUniversidad.ViewModels;

namespace AppParaUniversidad;

public partial class OptionsWindow : Window
{
    private readonly AppSettingsService _settingsService;
    private readonly AppSettings _settings;

    public OptionsWindow()
    {
        InitializeComponent();
        _settingsService = new AppSettingsService();
        _settings = _settingsService.Load();
        ThemeManager.ApplyTheme(_settings.DarkTheme);
        ApplyWindowSize(_settings.WindowWidth, _settings.WindowHeight);

        var directoryRepository = new DirectoryRepository();
        var emailTemplateService = new EmailTemplateService();
        var gmailService = CreateGmailService();
        var sendViewModel = new SendViewModel(directoryRepository, emailTemplateService, gmailService, CreateGmailService);
        var settingsViewModel = new SettingsViewModel(_settingsService, ApplyWindowSize, sendViewModel);
        SettingsViewControl.DataContext = settingsViewModel;
    }

    private IGmailService CreateGmailService()
    {
        return new GmailService();
    }

    private void ApplyWindowSize(double width, double height)
    {
        if (width > 600) Width = width;
        if (height > 400) Height = height;
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
