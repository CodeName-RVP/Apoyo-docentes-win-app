using System.IO;

namespace AppParaUniversidad.Services.Mail;

public static class GmailCredentialHelper
{
    private const string AppFolder = "AppParaUniversidad";
    private const string CredentialsFileName = "client_secret.json";

    public static string CredentialPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolder, CredentialsFileName);

    public static bool HasCredential() => System.IO.File.Exists(CredentialPath);

    public static void CopyCredential(string sourcePath)
    {
        var destDir = Path.GetDirectoryName(CredentialPath);
        if (!string.IsNullOrEmpty(destDir) && !System.IO.Directory.Exists(destDir))
        {
            System.IO.Directory.CreateDirectory(destDir);
        }
        System.IO.File.Copy(sourcePath, CredentialPath, overwrite: true);
    }
}
