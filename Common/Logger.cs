using System;
using System.IO;
using System.Text.RegularExpressions;

namespace AppParaUniversidad.Common;

public static class Logger
{
    private static readonly object LockObj = new();
    private static string LogFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AppParaUniversidad",
        "app.log");

    public static void LogError(string source, Exception ex)
    {
        try
        {
            var line = $"{DateTime.UtcNow:O}\t{source}\t{ex.GetType().Name}\t{Sanitize(ex.Message)}";
            WriteLine(line);
        }
        catch
        {
            // best effort, avoid crashing
        }
    }

    private static string Sanitize(string? message)
    {
        var value = message ?? string.Empty;
        value = Regex.Replace(value, @"[\r\n\t]+", " ");
        value = Regex.Replace(value, @"(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", "[correo-oculto]");
        return value.Length <= 512 ? value : value[..512] + "...";
    }

    private static void WriteLine(string line)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            lock (LockObj)
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // ignore logging failures
        }
    }
}
