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
using Google.Apis;
using MimeKit;

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
        GmailReady
            ? (string.IsNullOrWhiteSpace(AccountEmail)
                ? "Cuenta de Google vinculada"
                : $"El correo fue vinculado a la cuenta: {AccountEmail}")
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

    public async Task RefreshContactsAsync()
    {
        await UpdateFromLoadsAsync(_cargas.Values.ToList());
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

    private static string GetFriendlySendError(Exception ex)
    {
        return ex switch
        {
            ParseException => 
                "Error: Verifica la dirección.",
            FormatException => 
                "Error: Correo inválido.",
            Google.GoogleApiException googleEx when googleEx.HttpStatusCode == System.Net.HttpStatusCode.Unauthorized =>
                "Error: No autorizado. Verifica tu cuenta de Google.",
            Google.GoogleApiException googleEx when googleEx.HttpStatusCode == System.Net.HttpStatusCode.Forbidden =>
                "Error: Acceso denegado. Verifica los permisos de tu cuenta de Google.",
            Google.GoogleApiException googleEx when googleEx.HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests =>
                "Error: Demasiadas solicitudes. Intenta más tarde.",
            System.Net.Http.HttpRequestException =>
                "Error: Error de red. Verifica tu conexión a Internet.",
            TaskCanceledException =>
                "Error: La operación fue cancelada. Intenta nuevamente.",
            _ => $"Error inesperado al enviar correo: {ex.GetType().Name}: {ex.Message}"
        };
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
                r.Estado = "Enviando...";

                if (!_cargas.TryGetValue(r.NombreNormalizado, out var carga))
                {
                    Logs.Add(new SendLog(DateTime.UtcNow, r.NombreNormalizado, r.NombreVisible, r.Correo ?? string.Empty, "Skipped", "Sin carga"));
                    continue;
                }

                var html = _templateService.BuildHtml(carga, Ciclo, IntroText, FooterText);
                var subject = $"Carga docente - {Ciclo}";
                await _gmailService.SendAsync(r.Correo!, subject, html, CcEmail);

                r.Estado = "Enviado";

                Logs.Add(new SendLog(
                    DateTime.UtcNow, 
                    r.NombreNormalizado, 
                    r.NombreVisible, 
                    r.Correo!, 
                    "Sent", 
                    null));
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(SendAsync), ex);

                r.Estado = GetFriendlySendError(ex);
                
                Logs.Add(new SendLog(
                    DateTime.UtcNow, 
                    r.NombreNormalizado, 
                    r.NombreVisible, 
                    r.Correo ?? string.Empty, 
                    "Failed", 
                    GetFriendlySendError(ex)));
            }
        }

        var sent = Logs.Count(l => l.Estado == "Sent");
        var failed = Logs.Count(l => l.Estado == "Failed");
        var skipped = Logs.Count(l => l.Estado == "Skipped");
        Status = $"Envío finalizado. Enviados: {sent}, errores: {failed}, omitidos: {skipped}.";
    }

    private static int ScoreSuggestion(string cargaNorm, string contactoNorm)
    {
        var carga = NormalizeForSuggestion(cargaNorm);
        var contacto = NormalizeForSuggestion(contactoNorm);

        if (carga == contacto) return 0;

        var tokensCarga = carga.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tokensContacto = contacto.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var overlap = tokensCarga.Intersect(tokensContacto).Count();

        var sortedCarga = string.Join(' ', tokensCarga.OrderBy(x => x));
        var sortedContacto = string.Join(' ', tokensContacto.OrderBy(x => x));

        var distance = StringSimilarity.LevenshteinDistance(sortedCarga, sortedContacto);

        return distance - (overlap * 10);
    }

    private static string NormalizeForSuggestion(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = value
            .Trim()
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD);

        var chars = normalized
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) 
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray();

        return new string(chars)
            .Normalize(System.Text.NormalizationForm.FormC)
            .Replace("  ", " ");
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
            GmailStatus = GmailReady
                ? "Cuenta de Google vinculada"
                : "No se pudo inicializar Gmail.";
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(LoadCredentialAsync), ex);
            GmailReady = false;
            GmailStatus = $"ERROR: {ex.GetType().Name}: {ex.Message}";
        }
    }

    private async Task InitAccountEmailAsync()
    {
        try
        {
            await _gmailService.InitializeAsync().ConfigureAwait(true);

            // La autenticación funcionó.
            GmailReady = true;
            GmailStatus = "Cuenta de Google vinculada";

            // Obtener el correo es opcional.
            try
            {
                AccountEmail = await _gmailService.GetAccountEmailAsync()
                    .ConfigureAwait(true)
                    ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(AccountEmail))
                {
                    GmailStatus = $"Cuenta vinculada: {AccountEmail}";
                }
            }
            catch (Exception ex)
            {
                // No consideramos que Gmail esté desconectado
                // solo porque no podamos obtener el email.
                Logger.LogError(nameof(InitAccountEmailAsync), ex);
                AccountEmail = string.Empty;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(InitAccountEmailAsync), ex);

            GmailReady = false;
            AccountEmail = string.Empty;
            GmailStatus = "Cuenta de Google no vinculada";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
