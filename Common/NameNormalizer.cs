using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AppParaUniversidad.Common;

public static class NameNormalizer
{
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var withoutAccents = string.Concat(normalized.Where(c =>
            CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

        return Regex.Replace(withoutAccents, @"\s+", " ").Trim();
    }
}
