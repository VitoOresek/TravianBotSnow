using MainCore.UI.ViewModels.Tabs;
using ReactiveUI;
using ReactiveUI.Avalonia;
using System.Reactive.Disposables.Fluent;

namespace AvaloniaUI.Views.Tabs
{
    public class DebugTabBase : ReactiveUserControl<DebugViewModel>
    {
    }

    public partial class DebugTab : DebugTabBase
    {
        public DebugTab()
        {
            InitializeComponent();
            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.Logs, v => v.LogView.Text).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.Tasks, v => v.TaskView.ItemsSource).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.LeftCommand, v => v.ReportButton).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.RightCommand, v => v.LogButton).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.EndpointAddress, v => v.DevToolsEndpointAddress.Text).DisposeWith(d);
            });
        }
    }
}
