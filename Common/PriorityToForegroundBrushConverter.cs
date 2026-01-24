using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AppParaUniversidad.Common;

public sealed class PriorityToForegroundBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value?.ToString() ?? string.Empty;
        if (key == "Alta" || key == "Media")
        {
            return Brushes.Black;
        }

        if (App.Current.Resources["PriorityForegroundBrush"] is Brush brush)
        {
            return brush;
        }

        return Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
