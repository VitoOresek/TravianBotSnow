using Avalonia.Data.Converters;
using Avalonia.Media;
using Splat;
using System.Globalization;

namespace AvaloniaUI.Converter
{
    public class SplatColorToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not SplatColor color) return null;
            return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
