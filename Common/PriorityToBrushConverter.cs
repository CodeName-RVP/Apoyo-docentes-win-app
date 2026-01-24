using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AppParaUniversidad.Common;

public sealed class PriorityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value?.ToString() ?? string.Empty;
        return key switch
        {
            "Alta" => GetBrush("HighPriorityBrush"),
            "Media" => GetBrush("MediumPriorityBrush"),
            "Baja" => GetBrush("LowPriorityBrush"),
            _ => GetBrush("LowPriorityBrush")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static Brush GetBrush(string key)
    {
        if (App.Current.Resources[key] is Brush brush)
        {
            return brush;
        }

        return Brushes.Transparent;
    }
}
