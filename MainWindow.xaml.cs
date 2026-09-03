using System;
using System.Windows;
using System.Windows.Input;
using AppParaUniversidad.Services.Excel;
using AppParaUniversidad.Services.Directory;
using AppParaUniversidad.Services.Export;
using AppParaUniversidad.Services.Loads;
using AppParaUniversidad.Services.Email;
using AppParaUniversidad.Services.Mail;
using AppParaUniversidad.Services.Settings;
using AppParaUniversidad.Common;
using AppParaUniversidad.ViewModels;

namespace AppParaUniversidad;

public partial class MainWindow : Window
{
    private readonly IExcelReader _excelReader;
    private readonly ITeachingLoadBuilder _teachingLoadBuilder;
    private readonly IDirectoryRepository _directoryRepository;
    private readonly SendViewModel _sendViewModel;
    private readonly LoadsViewModel _loadsViewModel;
    private readonly DirectoryViewModel _directoryViewModel;
    private readonly ImportExportViewModel _importExportViewModel;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IGmailService _gmailService;
    private readonly AppSettingsService _settingsService;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly AppSettings _settings;

    public MainWindow()
    {
        InitializeComponent();
        _excelReader = new ExcelReader();
        _teachingLoadBuilder = new TeachingLoadBuilder();
        _directoryRepository = new DirectoryRepository();
        _emailTemplateService = new EmailTemplateService();
        _gmailService = CreateGmailService();
        _settingsService = new AppSettingsService();
        _settings = _settingsService.Load();
        _sendViewModel = new SendViewModel(_directoryRepository, _emailTemplateService, _gmailService, CreateGmailService);
        _loadsViewModel = new LoadsViewModel(_excelReader, _teachingLoadBuilder);
        _directoryViewModel = new DirectoryViewModel(_directoryRepository);
        _importExportViewModel = new ImportExportViewModel(_directoryRepository, new JsonExportService(), _sendViewModel);
        _settingsViewModel = new SettingsViewModel(_settingsService, ApplyWindowSize, _sendViewModel);

        _loadsViewModel.GroupedUpdated += loads => _ = SafeUpdateSendView(loads);
        _directoryViewModel.ContactsChanged += (_, _) => _ = _sendViewModel.RefreshContactsAsync();

        LoadsViewControl.DataContext = _loadsViewModel;
        DirectoryViewControl.DataContext = _directoryViewModel;
        SendViewControl.DataContext = _sendViewModel;
        ImportExportViewControl.DataContext = _importExportViewModel;
        SettingsViewControl.DataContext = _settingsViewModel;

        ApplyWindowSize(_settings.WindowWidth, _settings.WindowHeight);
        ThemeManager.ApplyTheme(_settings.DarkTheme);
    }

    private async Task SafeUpdateSendView(IReadOnlyList<Domain.Models.DocenteCarga> loads)
    {
        try
        {
            await _sendViewModel.UpdateFromLoadsAsync(loads);
        }
        catch (Exception ex)
        {
            Common.Logger.LogError(nameof(SafeUpdateSendView), ex);
        }
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

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (Owner is StartMenuWindow menu)
        {
            menu.Show();
        }
    }

    public void OpenSettingsTab()
    {
        MainTabs.SelectedIndex = 4;
    }
}
