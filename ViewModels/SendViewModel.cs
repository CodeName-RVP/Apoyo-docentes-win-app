using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AppParaUniversidad.Common;
using AppParaUniversidad.Domain.Models;
using AppParaUniversidad.Services.Directory;
using AppParaUniversidad.Services.Email;
using AppParaUniversidad.Services.Mail;

namespace AppParaUniversidad.ViewModels;

public class SendViewModel : INotifyPropertyChanged
{
    private readonly IDirectoryRepository _directoryRepository;
    private readonly IEmailTemplateService _templateService;
    private IGmailService _gmailService;
    private readonly Func<IGmailService>? _gmailFactory;

    private string _status = "Sin datos";
    private string _gmailStatus = "Credenciales no verificadas";
    private bool _gmailReady;
    private DocenteDeliveryStatus? _selected;
    private string _previewHtml = "Seleccione un docente para previsualizar.";
    private string _ciclo = "Ciclo actual";
    private string _introText = "Apreciable %maestro% se le hace la entrega de su plan de estudio %plan de estudio actual%";
    private string _footerText = "Atentamente, Coordinacion Academica";
    private string? _ccEmail;
    private string _accountEmail = string.Empty;
    private readonly Dictionary<string, DocenteCarga> _cargas = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<DocenteDeliveryStatus> Recipients { get; } = new();
    public ObservableCollection<SendLog> Logs { get; } = new();
    public ObservableCollection<string> AvailableEmails { get; } = new();

    public RelayCommand SelectValidCommand { get; }
    public RelayCommand DeselectAllCommand { get; }
    public AsyncRelayCommand SendCommand { get; }
    public RelayCommand PreviewCommand { get; }
    public AsyncRelayCommand SaveToDirectoryCommand { get; }
    public AsyncRelayCommand LoadCredentialCommand { get; }
    public RelayCommand ApplySuggestionCommand { get; }

    public string Status
    {
        get => _status;
        private set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    public string GmailStatus
    {
        get => _gmailStatus;
        private set
        {
            if (_gmailStatus != value)
            {
                _gmailStatus = value;
                OnPropertyChanged();
            }
        }
    }

    public string AccountEmail
    {
        get => _accountEmail;
        private set
        {
            if (_accountEmail != value)
            {
                _accountEmail = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GmailButtonText));
            }
        }
    }

    public string GmailButtonText =>
        GmailReady && !string.IsNullOrWhiteSpace(AccountEmail)
            ? $"El correo fue vinculado a la cuenta: {AccountEmail}"
            : "Agregar cuenta Gmail";

    public bool GmailReady
    {
        get => _gmailReady;
        private set
        {
            if (_gmailReady != value)
            {
                _gmailReady = value;
                OnPropertyChanged();
                SendCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    public string Ciclo
    {
        get => _ciclo;
        set
        {
            if (_ciclo != value)
            {
                _ciclo = value;
                OnPropertyChanged();
            }
        }
    }

    public string IntroText
    {
        get => _introText;
        set
        {
            if (_introText != value)
            {
                _introText = value;
                OnPropertyChanged();
            }
        }
    }

    public string FooterText
    {
        get => _footerText;
        set
        {
            if (_footerText != value)
            {
                _footerText = value;
                OnPropertyChanged();
            }
        }
    }

    public string? CcEmail
    {
        get => _ccEmail;
        set
        {
            if (_ccEmail != value)
            {
                _ccEmail = value;
                OnPropertyChanged();
            }
        }
    }

    public DocenteDeliveryStatus? SelectedRecipient
    {
        get => _selected;
        set
        {
            if (_selected != value)
            {
                _selected = value;
                OnPropertyChanged();
                PreviewCommand.RaiseCanExecuteChanged();
                SaveToDirectoryCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string PreviewHtml
    {
        get => _previewHtml;
        private set
        {
            if (_previewHtml != value)
            {
                _previewHtml = value;
                OnPropertyChanged();
            }
        }
    }

    public SendViewModel(
        IDirectoryRepository directoryRepository,
        IEmailTemplateService templateService,
        IGmailService gmailService,
        Func<IGmailService>? gmailFactory = null)
    {
        _directoryRepository = directoryRepository;
        _templateService = templateService;
        _gmailService = gmailService;
        _gmailFactory = gmailFactory;
        GmailReady = false;
        GmailStatus = "Cuenta de Google no vinculada";

        SelectValidCommand = new RelayCommand(_ => SelectValidRecipients(), _ => Recipients.Any());
        DeselectAllCommand = new RelayCommand(_ => DeselectAll(), _ => Recipients.Any());
        SendCommand = new AsyncRelayCommand(SendAsync, () => GmailReady && Recipients.Any(r => r.Enviar && r.TieneCorreo));
        PreviewCommand = new RelayCommand(_ => BuildPreview(), _ => SelectedRecipient is not null);
        SaveToDirectoryCommand = new AsyncRelayCommand(SaveSelectedToDirectoryAsync, () => SelectedRecipient is not null);
        LoadCredentialCommand = new AsyncRelayCommand(LoadCredentialAsync);
        ApplySuggestionCommand = new RelayCommand(param => ApplySuggestion(param));
        _ = InitAccountEmailAsync();
    }

    public async Task UpdateFromLoadsAsync(IReadOnlyList<DocenteCarga> loads)
    {
        var contacts = await _directoryRepository.GetAllAsync();
        _cargas.Clear();
        foreach (var carga in loads)
        {
            _cargas[carga.NombreNormalizado] = carga;
        }

        var allEmails = contacts.Where(c => c.Activo && !string.IsNullOrWhiteSpace(c.Correo))
                                .Select(c => c.Correo.Trim())
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();
        AvailableEmails.Clear();
        foreach (var email in allEmails)
        {
            AvailableEmails.Add(email);
        }

        Recipients.Clear();

        foreach (var carga in loads)
        {
            var match = contacts.FirstOrDefault(c => c.NombreNormalizado == carga.NombreNormalizado && c.Activo);
            var item = new DocenteDeliveryStatus
            {
                NombreVisible = carga.NombreVisible,
                NombreNormalizado = carga.NombreNormalizado,
                Correo = match?.Correo,
                TieneCorreo = match is not null,
                Sugerencias = contacts.Where(c => c.Activo)
                    .Select(c => new
                    {
                        Nombre = c.Correo,
                        Score = ScoreSuggestion(carga.NombreNormalizado, c.NombreNormalizado)
                    })
                    .OrderBy(x => x.Score)
                    .Take(5)
                    .Select(x => x.Nombre)
                    .ToList(),
                CorreosDisponibles = allEmails,
                Enviar = match is not null
            };
            item.PropertyChanged += RecipientChanged;
            Recipients.Add(item);
        }

        Status = $"Docentes detectados: {Recipients.Count}, con correo: {Recipients.Count(r => r.TieneCorreo)}";
        SelectValidCommand.RaiseCanExecuteChanged();
        DeselectAllCommand.RaiseCanExecuteChanged();
        SendCommand.RaiseCanExecuteChanged();
        PreviewCommand.RaiseCanExecuteChanged();
        SaveToDirectoryCommand.RaiseCanExecuteChanged();
    }

    private void ApplySuggestion(object? param)
    {
        if (param is not DocenteDeliveryStatus r) return;
        var best = r.Sugerencias.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(best)) return;
        r.Correo = best;
        r.TieneCorreo = true;
        r.Enviar = true;
        SendCommand.RaiseCanExecuteChanged();
        DeselectAllCommand.RaiseCanExecuteChanged();
    }

    private void SelectValidRecipients()
    {
        foreach (var r in Recipients)
        {
            r.Enviar = r.TieneCorreo;
        }
        OnPropertyChanged(nameof(Recipients));
        SendCommand.RaiseCanExecuteChanged();
        DeselectAllCommand.RaiseCanExecuteChanged();
    }

    private void DeselectAll()
    {
        foreach (var r in Recipients)
        {
            r.Enviar = false;
        }
        SendCommand.RaiseCanExecuteChanged();
        DeselectAllCommand.RaiseCanExecuteChanged();
    }

    private void RecipientChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not DocenteDeliveryStatus r) return;
        if (e.PropertyName == nameof(DocenteDeliveryStatus.Correo))
        {
            r.TieneCorreo = !string.IsNullOrWhiteSpace(r.Correo);
            if (!r.TieneCorreo)
            {
                r.Enviar = false;
            }
        }
        SendCommand.RaiseCanExecuteChanged();
        PreviewCommand.RaiseCanExecuteChanged();
        SaveToDirectoryCommand.RaiseCanExecuteChanged();
        DeselectAllCommand.RaiseCanExecuteChanged();
    }

    private void BuildPreview()
    {
        if (SelectedRecipient is null)
        {
            PreviewHtml = "Seleccione un docente para previsualizar.";
            return;
        }

        if (!_cargas.TryGetValue(SelectedRecipient.NombreNormalizado, out var carga))
        {
            PreviewHtml = "No hay carga asociada para este docente.";
            return;
        }

        var html = _templateService.BuildHtml(carga, Ciclo, IntroText, FooterText);
        PreviewHtml = html;
    }

    private async Task SaveSelectedToDirectoryAsync()
    {
        if (SelectedRecipient is null) return;
        try
        {
            if (string.IsNullOrWhiteSpace(SelectedRecipient.Correo))
            {
                Status = "No hay correo para guardar.";
                return;
            }

            var contact = new Contact
            {
                NombreVisible = SelectedRecipient.NombreVisible,
                NombreNormalizado = SelectedRecipient.NombreNormalizado,
                Correo = SelectedRecipient.Correo,
                Telefono = null,
                Activo = true
            };
            await _directoryRepository.UpsertAsync(contact);
            Status = "Correo guardado en directorio.";
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(SaveSelectedToDirectoryAsync), ex);
            Status = "Error al guardar correo.";
        }
    }

    private async Task SendAsync()
    {
        Logs.Clear();
        var selected = Recipients.Where(r => r.Enviar && r.TieneCorreo).ToList();
        if (selected.Count == 0)
        {
            Status = "No hay destinatarios validos seleccionados.";
            return;
        }

        foreach (var r in selected)
        {
            try
            {
                if (!_cargas.TryGetValue(r.NombreNormalizado, out var carga))
                {
                    Logs.Add(new SendLog(DateTime.UtcNow, r.NombreNormalizado, r.NombreVisible, r.Correo ?? string.Empty, "Skipped", "Sin carga"));
                    continue;
                }

                var html = _templateService.BuildHtml(carga, Ciclo, IntroText, FooterText);
                var subject = $"Carga docente - {Ciclo}";
                await _gmailService.SendAsync(r.Correo!, subject, html, CcEmail);

                Logs.Add(new SendLog(DateTime.UtcNow, r.NombreNormalizado, r.NombreVisible, r.Correo!, "Sent", null));
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(SendAsync), ex);
                Logs.Add(new SendLog(DateTime.UtcNow, r.NombreNormalizado, r.NombreVisible, r.Correo ?? string.Empty, "Failed", ex.Message));
            }
        }

        var sent = Logs.Count(l => l.Estado == "Sent");
        var failed = Logs.Count(l => l.Estado == "Failed");
        var skipped = Logs.Count(l => l.Estado == "Skipped");
        Status = $"Envio finalizado. Exitos: {sent}, Fallos: {failed}, Skipped: {skipped}";
    }

    private static int ScoreSuggestion(string cargaNorm, string contactoNorm)
    {
        var tokensCarga = cargaNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tokensContacto = contactoNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var overlap = tokensCarga.Intersect(tokensContacto).Count();
        var distance = StringSimilarity.LevenshteinDistance(cargaNorm, contactoNorm);
        return distance - (overlap * 2);
    }

    private async Task LoadCredentialAsync()
    {
        try
        {
            GmailStatus = "Abriendo Google para vincular la cuenta...";

            await Services.Google.GoogleAuthService.Shared.InitializeAsync().ConfigureAwait(true);

            if (_gmailFactory != null)
            {
                _gmailService = _gmailFactory();
            }

            await _gmailService.InitializeAsync().ConfigureAwait(true);

            GmailReady = _gmailService is not NullGmailService;
            AccountEmail = await _gmailService.GetAccountEmailAsync().ConfigureAwait(true) ?? string.Empty;
            GmailStatus = GmailReady
                ? "Cuenta de Google vinculada"
                : "No se pudo inicializar Gmail.";
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(LoadCredentialAsync), ex);
            GmailReady = false;
            GmailStatus = "No se pudo vincular la cuenta de Google.";
        }
    }

    private async Task InitAccountEmailAsync()
    {
        if (!GmailReady) return;
        try
        {
            AccountEmail = await _gmailService.GetAccountEmailAsync() ?? string.Empty;
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(InitAccountEmailAsync), ex);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
