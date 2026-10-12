using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaUI.Views;
using MainCore;
using MainCore.Services;
using MainCore.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Splat.Microsoft.Extensions.DependencyInjection;
using Splat.ModeDetection;

namespace AvaloniaUI
{
    public partial class App : Application
    {
        private IHost? _host;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Splat.ModeDetector.OverrideModeDetector(Mode.Run);

                // built here, after Avalonia set up ReactiveUI, so its registrations land in the DI container
                var host = AppMixins.GetHostBuilder()
                    .ConfigureServices(services => services.AddSingleton<MainWindow>())
                    .Build();
                _host = host;

                var serviceProvider = host.Services;
                serviceProvider.UseMicrosoftDependencyResolver();
                RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
                RxApp.DefaultExceptionHandler = serviceProvider.GetRequiredService<ObservableExceptionHandler>();

                var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
                DialogHandlers.Register(serviceProvider.GetRequiredService<IDialogService>(), mainWindow);
                serviceProvider.GetRequiredService<IRxQueue>().Setup();

                // Ctrl+C or SIGTERM: close the window so the browsers are shut down cleanly
                serviceProvider.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping
                    .Register(() => Dispatcher.UIThread.Post(mainWindow.Close));

                desktop.MainWindow = mainWindow;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                desktop.Exit += (_, _) =>
                {
                    host.StopAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
                    host.Dispose();
                };

                _ = host.StartAsync();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
