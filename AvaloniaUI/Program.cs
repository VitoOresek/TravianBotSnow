using Avalonia;
using ReactiveUI.Avalonia;

namespace AvaloniaUI
{
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // the database, logs, chrome profiles and screenshots use relative paths, keep them next to the binary
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // also used by the Avalonia designer
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();
    }
}
