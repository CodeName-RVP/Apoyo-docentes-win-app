using System;
using System.Windows;
using System.Windows.Input;
using AppParaUniversidad.Common;
using AppParaUniversidad.Services.Directory;
using AppParaUniversidad.Services.Email;
using AppParaUniversidad.Services.Mail;
using AppParaUniversidad.Services.Schedules;
using AppParaUniversidad.Services.Settings;
using AppParaUniversidad.Services.Sheets;
using AppParaUniversidad.ViewModels;

namespace AppParaUniversidad;

public partial class TeacherSchedulesWindow : Window
{
    private readonly AppSettingsService _settingsService;
    private readonly AppSettings _settings;
    private bool _settingsVisible;
    private bool _sheetHistoryVisible;

    public TeacherSchedulesWindow()
    {
        InitializeComponent();
        _settingsService = new AppSettingsService();
        _settings = _settingsService.Load();
        ThemeManager.ApplyTheme(_settings.DarkTheme);
        ApplyWindowSize(_settings.WindowWidth, _settings.WindowHeight);

        var fileReader = new ScheduleFileReader();
        var emailTemplateService = new EmailTemplateService();
        var gmailService = CreateGmailService();
        var sheetReader = new ScheduleSheetsReader();
        var historyRepository = new SheetHistoryRepository();
        var directoryRepository = new DirectoryRepository();
        var sendViewModel = new SendViewModel(directoryRepository, emailTemplateService, gmailService, CreateGmailService);
        var settingsViewModel = new SettingsViewModel(_settingsService, ApplyWindowSize, sendViewModel);

        SchedulesViewControl.DataContext = new ScheduleViewModel(fileReader, sheetReader, historyRepository, directoryRepository);
        SheetHistoryViewControl.DataContext = SchedulesViewControl.DataContext;
        SettingsViewControl.DataContext = settingsViewModel;
    }

    private IGmailService CreateGmailService()
    {
        try
        {
            return new GmailService();
        }
        catch
        {
            return new NullGmailService("Falta client_secret.json en %AppData%/AppParaUniversidad");
        }
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

    private void OnSchedulesClick(object sender, RoutedEventArgs e)
    {
        _settingsVisible = false;
        _sheetHistoryVisible = false;
        SchedulesPanel.Visibility = Visibility.Visible;
        SettingsPanel.Visibility = Visibility.Collapsed;
        SheetHistoryPanel.Visibility = Visibility.Collapsed;
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        _settingsVisible = !_settingsVisible;
        _sheetHistoryVisible = false;
        SettingsPanel.Visibility = _settingsVisible ? Visibility.Visible : Visibility.Collapsed;
        SheetHistoryPanel.Visibility = Visibility.Collapsed;
        SchedulesPanel.Visibility = _settingsVisible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnSheetHistoryClick(object sender, RoutedEventArgs e)
    {
        _sheetHistoryVisible = !_sheetHistoryVisible;
        _settingsVisible = false;
        SheetHistoryPanel.Visibility = _sheetHistoryVisible ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Collapsed;
        SchedulesPanel.Visibility = _sheetHistoryVisible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (Owner is StartMenuWindow menu)
        {
            menu.Show();
        }
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
}
