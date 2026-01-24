using System;
using System.Linq;
using System.Windows;

namespace AppParaUniversidad.Common;

public static class ThemeManager
{
    public static void ApplyTheme(bool dark)
    {
        var res = Application.Current.Resources;
        var merged = res.MergedDictionaries;
        var themeSource = new Uri(dark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", UriKind.Relative);
        var existing = merged.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Themes/"));

        if (existing != null && existing.Source == themeSource)
        {
            return;
        }

        if (existing != null)
        {
            merged.Remove(existing);
        }

        merged.Insert(0, new ResourceDictionary { Source = themeSource });
    }
}
