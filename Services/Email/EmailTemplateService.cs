using System.Text;
using AppParaUniversidad.Domain.Models;

namespace AppParaUniversidad.Services.Email;

public sealed class EmailTemplateService : IEmailTemplateService
{
    public string BuildHtml(DocenteCarga carga, string ciclo, string introText, string footerText)
    {
        var sb = new StringBuilder();
        sb.Append("""
<html>
<head>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
<style>
body { font-family: 'Segoe UI', Arial, sans-serif; color:#222; }
table { width:100%; border-collapse:collapse; margin-top:10px; }
th { text-align:left; background:#f0f3f8; }
th, td { padding:8px 10px; border-bottom:1px solid #e6e9f0; font-size:13px; }
.total { font-weight:bold; background:#f9fafc; }
.label { font-weight:600; margin-top:6px; }
</style>
</head>
<body>
  <div style="font-size:18px; font-weight:600;">Carga Docente</div>
  <div class="label">Docente:</div>
  <div>{{DOCENTE}}</div>
  <div style="margin-top:8px;">{{INTRO}}</div>
  <div class="label" style="margin-top:8px;">Ciclo:</div>
  <div>{{CICLO}}</div>
  <table>
    <tr>
      <th>Asignatura</th><th>Grupo</th><th>Horas</th><th>Salon</th>
      <th>Lu</th><th>Ma</th><th>Mi</th><th>Ju</th><th>Vi</th>
      <th>Tipo</th><th>Comision</th>
    </tr>
""".Replace("{{DOCENTE}}", Escape(carga.NombreVisible))
  .Replace("{{CICLO}}", Escape(ciclo))
  .Replace("{{INTRO}}", Escape(RenderIntro(introText, carga.NombreVisible, ciclo))));

        foreach (var item in carga.Items)
        {
            sb.Append("<tr>");
            sb.Append(Cell(item.Asignatura));
            sb.Append(Cell(item.Grupo));
            sb.Append(Cell(item.Horas.ToString("0.##")));
            sb.Append(Cell(item.Salon));
            sb.Append(Cell(item.Lu));
            sb.Append(Cell(item.Ma));
            sb.Append(Cell(item.Mi));
            sb.Append(Cell(item.Ju));
            sb.Append(Cell(item.Vi));
            sb.Append(Cell(item.Tipo));
            sb.Append(Cell(item.Comision));
            sb.Append("</tr>");
        }

        sb.Append("<tr class='total'><td colspan='2'>Total</td>");
        sb.Append(Cell(carga.TotalHoras.ToString("0.##")));
        sb.Append("<td colspan='8'></td></tr>");
        sb.Append("""
    </table>
    <div style="margin-top:10px;">{{FOOTER}}</div>
</body>
</html>
""".Replace("{{FOOTER}}", Escape(footerText)));
        return sb.ToString();
    }

    private static string Cell(string? text) => $"<td>{Escape(text)}</td>";

    private static string Escape(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return System.Net.WebUtility.HtmlEncode(text);
    }

    private static string RenderIntro(string introText, string docente, string ciclo)
    {
        var text = introText ?? string.Empty;
        text = text.Replace("%maestro%", docente, StringComparison.OrdinalIgnoreCase);
        text = text.Replace("%plan de estudio actual%", ciclo, StringComparison.OrdinalIgnoreCase);
        return text;
    }
}
