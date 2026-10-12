using Avalonia.Data.Converters;
using MainCore.UI.Models.Output;
using System.Globalization;

namespace AvaloniaUI.Converter
{
    public class TribeItemToCroppedBitmap : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not TribeItem item) return null;
            return CroppedImageLoader.Load(item.ImageSource, TribeItem.ImageMask);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
