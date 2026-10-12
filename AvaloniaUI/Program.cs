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

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                Serilog.Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception, the bot is closing");
                Serilog.Log.CloseAndFlush();
            };
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Serilog.Log.Error(e.Exception, "Unobserved task exception");
                e.SetObserved();
            };

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
