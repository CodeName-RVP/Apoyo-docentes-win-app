using AppParaUniversidad.Services.Updates;

namespace AppParaUniversidad.Common;

public static class UpdateCheckState
{
    public static bool HasChecked { get; set; }
    public static bool CheckSucceeded { get; set; }
    public static bool UpdateAvailable { get; set; }
    public static string StatusText { get; set; } = "Verificando...";
    public static GitHubUpdateInfo? Release { get; set; }
}
