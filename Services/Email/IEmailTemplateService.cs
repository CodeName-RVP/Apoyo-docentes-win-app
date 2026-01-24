using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Email;

public interface IEmailTemplateService
{
    string BuildHtml(DocenteCarga carga, string ciclo, string introText, string footerText);
}
