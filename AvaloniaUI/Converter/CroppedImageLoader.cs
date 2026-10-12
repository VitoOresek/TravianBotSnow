using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Collections.Concurrent;

namespace AvaloniaUI.Converter
{
    /// <summary>
    /// MainCore describes images with WPF pack URIs (pack://application:,,,/Resources/x.png),
    /// they are served from the Assets folder of this assembly instead.
    /// </summary>
    internal static class CroppedImageLoader
    {
        private static readonly ConcurrentDictionary<string, Bitmap> _bitmaps = new();

        public static CroppedBitmap? Load(string? path, System.Drawing.Rectangle mask)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var fileName = path[(path.LastIndexOf('/') + 1)..];
            var bitmap = _bitmaps.GetOrAdd(fileName, name => new Bitmap(AssetLoader.Open(new Uri($"avares://TravianBotSharp/Assets/{name}"))));
            return new CroppedBitmap(bitmap, new PixelRect(mask.X, mask.Y, mask.Width, mask.Height));
        }
    }
}
