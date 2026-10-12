using Avalonia.Controls;
using MainCore.UI.ViewModels;
using MainCore.UI.ViewModels.UserControls;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Serilog;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace AvaloniaUI.Views
{
    public class MainWindowBase : ReactiveWindow<MainViewModel>
    {
    }

    public partial class MainWindow : MainWindowBase
    {
        private bool _canClose = false;
        private bool _isClosing = false;
        private bool _isLoaded = false;

        public MainWindow(MainViewModel mainViewModel, WaitingOverlayViewModel waitingOverlayViewModel)
        {
            InitializeComponent();
            Opened += OnOpened;
            Closing += OnClosing;

            ViewModel = mainViewModel;
            WaitingOverlay.ViewModel = waitingOverlayViewModel;

            // most likely Chrome is missing or the chromedriver download failed,
            // subscribing here keeps the generic RxApp handler from showing a second, vaguer dialog
            mainViewModel.LoadCommand.ThrownExceptions.Subscribe(async ex =>
            {
                Log.Error(ex, "Startup failed");
                await DialogHandlers.ShowMessage(this, "Startup failed", $"{ex.Message}\n\nFix the problem and restart the bot. Details are in logs/log-Other-*.txt", false);
            });

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.MainLayoutViewModel, v => v.MainLayout.ViewModel).DisposeWith(d);
            });
        }

        private async void OnOpened(object? sender, EventArgs e)
        {
            if (_isLoaded || ViewModel is null) return;
            try
            {
                await ViewModel.LoadCommand.Execute();
            }
            catch
            {
                // reported through LoadCommand.ThrownExceptions
            }
            finally
            {
                _isLoaded = true;
            }
        }

        private async void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            Log.Information("Main window closing ({Reason}, programmatic: {IsProgrammatic})", e.CloseReason, e.IsProgrammatic);
            if (!_isLoaded)
            {
                e.Cancel = true;
                return;
            }

            if (_canClose) return;
            e.Cancel = true;
            if (_isClosing) return;
            _isClosing = true;

            if (ViewModel is not null)
            {
                await ViewModel.UnloadCommand.Execute();
            }

            _canClose = true;
            Close();
        }
    }
}
