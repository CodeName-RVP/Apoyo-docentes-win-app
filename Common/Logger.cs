using System;
using System.IO;

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
            var line = $"{DateTime.UtcNow:O}\t{source}\t{ex.GetType().Name}\t{ex.Message}";
            WriteLine(line);
        }
        catch
        {
            // best effort, avoid crashing
        }
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
