using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MainCore.Services;
using System.Reactive;

namespace AvaloniaUI
{
    /// <summary>
    /// Answers the dialog interactions MainCore raises (the WPF build used MessageBox and Win32 file dialogs).
    /// </summary>
    public static class DialogHandlers
    {
        private static readonly FilePickerFileType TbsFiles = new("TBS files") { Patterns = ["*.tbs"] };

        public static void Register(IDialogService dialogService, Window owner)
        {
            dialogService.MessageBox.RegisterHandler(async context =>
            {
                await Dispatcher.UIThread.InvokeAsync(() => ShowMessage(owner, context.Input.Title, context.Input.Message, false));
                context.SetOutput(Unit.Default);
            });

            dialogService.ConfirmBox.RegisterHandler(async context =>
            {
                var result = await Dispatcher.UIThread.InvokeAsync(() => ShowMessage(owner, context.Input.Title, context.Input.Message, true));
                context.SetOutput(result);
            });

            dialogService.OpenFileDialog.RegisterHandler(async context =>
            {
                var result = await Dispatcher.UIThread.InvokeAsync(() => OpenFile(owner));
                context.SetOutput(result);
            });

            dialogService.SaveFileDialog.RegisterHandler(async context =>
            {
                var result = await Dispatcher.UIThread.InvokeAsync(() => SaveFile(owner));
                context.SetOutput(result);
            });
        }

        private static async Task<string> OpenFile(Window owner)
        {
            var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import",
                AllowMultiple = false,
                FileTypeFilter = [TbsFiles, FilePickerFileTypes.All],
                SuggestedStartLocation = await owner.StorageProvider.TryGetFolderFromPathAsync(AppContext.BaseDirectory),
            });
            if (files.Count == 0) return "";
            return files[0].TryGetLocalPath() ?? "";
        }

        private static async Task<string> SaveFile(Window owner)
        {
            var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export",
                DefaultExtension = "tbs",
                FileTypeChoices = [TbsFiles, FilePickerFileTypes.All],
                SuggestedStartLocation = await owner.StorageProvider.TryGetFolderFromPathAsync(AppContext.BaseDirectory),
            });
            if (file is null) return "";
            return file.TryGetLocalPath() ?? "";
        }

        public static async Task<bool> ShowMessage(Window owner, string title, string message, bool confirm)
        {
            var result = false;
            var window = new Window
            {
                Title = title,
                SizeToContent = SizeToContent.WidthAndHeight,
                MinWidth = 320,
                MaxWidth = 640,
                CanResize = false,
                ShowInTaskbar = false,
                WindowStartupLocation = owner.IsVisible ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
            };

            Button CreateButton(string text, bool value)
            {
                var button = new Button { Content = text, MinWidth = 80, HorizontalAlignment = HorizontalAlignment.Right };
                button.Click += (_, _) =>
                {
                    result = value;
                    window.Close();
                };
                return button;
            }

            if (confirm)
            {
                buttons.Children.Add(CreateButton("Yes", true));
                buttons.Children.Add(CreateButton("No", false));
            }
            else
            {
                buttons.Children.Add(CreateButton("OK", true));
            }

            window.Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 16,
                Children =
                {
                    new SelectableTextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    buttons,
                },
            };

            if (owner.IsVisible)
            {
                await window.ShowDialog(owner);
            }
            else
            {
                var closed = new TaskCompletionSource();
                window.Closed += (_, _) => closed.TrySetResult();
                window.Show();
                await closed.Task;
            }
            return result;
        }
    }
}
