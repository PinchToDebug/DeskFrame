using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using MenuItem = Wpf.Ui.Controls.MenuItem;
namespace DeskFrame
{
    public partial class SettingsWindow : FluentWindow
    {
        InstanceController _controller;
        DeskFrameWindow _dWindows;
        Instance _instance;
        MainWindow _window;
        ContextMenu ManageFrameContextMenu;
        public SettingsWindow(InstanceController controller, MainWindow window)
        {
            InitializeComponent();
            DragWinBorder.MouseEnter += (_, _) =>
            {
                DragWinBorder.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
            };
            DragWinBorder.MouseLeave += (_, _) =>
            {
                DragWinBorder.Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255));
            };

            this.LocationChanged += Window_LocationChanged;
            this.MinHeight = 0;
            this.MinWidth = 200;
            _window = window;
            _controller = controller;
            // if (_controller.reg.KeyExistsRoot("blurBackground")) blurToggle.IsChecked = (bool)_controller.reg.ReadKeyValueRoot("blurBackground");
            if (_controller.reg.KeyExistsRoot("AutoUpdate")) AutoUpdateToggleSwitch.IsChecked = (bool)_controller.reg.ReadKeyValueRoot("AutoUpdate");
            if (_controller.reg.KeyExistsRoot("DoubleClickToHide")) DoubleClickToHideSwitch.IsChecked = (bool)_controller.reg.ReadKeyValueRoot("DoubleClickToHide");
        }

        private void blurToggle_CheckChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            //   _controller.reg.WriteToRegistryRoot("blurBackground", blurToggle.IsChecked!);
            //   _controller.ChangeBlur((bool)blurToggle.IsChecked!);
        }

        private void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ExportRegistryKey(_controller.reg.regKeyName);
        }

        void ExportRegistryKey(string regKeyName)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Registry Files (*.reg)|*.reg",
                Title = "Export Registry Key",
                FileName = $"DeskFrame_settings_{DateTime.Now.ToString("yyyy-MM-dd_hhmm")}"
            };
            if (saveDialog.ShowDialog() == true)
            {
                string fullKeyPath = $@"HKCU\SOFTWARE\{regKeyName}";
                string arguments = $"export \"{fullKeyPath}\" \"{saveDialog.FileName}\" /y";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                }.Start();
            }
        }

        private void AutoUpdateToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)AutoUpdateToggleSwitch.IsChecked!)
            {

                _controller.reg.AddToAutoRun("DeskFrame", Process.GetCurrentProcess().MainModule!.FileName);
            }
            else
            {
                _controller.reg.RemoveFromAutoRun("DeskFrame");
            }
            _controller.reg.WriteToRegistryRoot("AutoUpdate", AutoUpdateToggleSwitch.IsChecked);
        }

        private void ManageFrameButton_Click(object sender, RoutedEventArgs e)
        {
            ManageFrameContextMenu = new ContextMenu();
            List<MenuItem> menuItems = new List<MenuItem>();
            foreach (var frame in _controller._subWindows)
            {
                MenuItem menuItem = new MenuItem
                {
                    Header = frame.title.Text,
                    Height = 34,
                    Icon = new SymbolIcon(SymbolRegular.Window20)
                };

                string originalBorderColor = frame.Instance.BorderColor;
                bool originalBorderState = frame.Instance.BorderEnabled;

                menuItem.Click += (_, _) =>
                {
                    var dialog = new FrameSettingsDialog(frame);
                    dialog.ShowDialog();
                    if (dialog.DialogResult == true)
                    {
                        frame.LoadFiles(frame._currentFolderPath);
                    }
                };
                menuItem.MouseEnter += (_, _) =>
                {
                    menuItem.Icon.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#7CFF00"));
                    frame.Instance.BorderEnabled = true;
                    frame.Instance.BorderColor = "#7CFF00";
                };
                menuItem.MouseLeave += (_, _) =>
                {
                    menuItem.Icon.Foreground = Brushes.White;
                    frame.Instance.BorderEnabled = originalBorderState;
                    frame.Instance.BorderColor = originalBorderColor;
                };
                menuItems.Add(menuItem);
            }
            foreach (var item in menuItems)
            {
                ManageFrameContextMenu.Items.Add(item);
            }
            ManageFrameContextMenu.IsOpen = true;
        }

        private void DefaultFrameStyleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_dWindows != null) _dWindows.Close();

            _instance = new Instance("Default Style", true);
            _instance.SettingDefault = true;
            _instance.Folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            _dWindows = new DeskFrameWindow(_instance);
            _dWindows.addFolder.Visibility = Visibility.Hidden;
            _dWindows.showFolder.Visibility = Visibility.Visible;
            _dWindows.title.Visibility = Visibility.Visible;
            _dWindows.WindowBorder.Visibility = Visibility.Visible;
            _dWindows.Left = this.Width + this.Left + 10;
            _dWindows.Top = this.Top;
            _dWindows.Show();

        }
        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (_dWindows != null)
            {
                _dWindows.Left = this.Width + this.Left + 10;
                _dWindows.Top = this.Top;
            }
        }
        private void ResetDefaultFrameStyleButton_Click(object sender, RoutedEventArgs e)
        {
            string[] keep = { "AutoUpdate", "blurBackground", "startOnLogin" };
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\DeskFrame", writable: true)!;
            foreach (var name in key.GetValueNames())
            {
                if (Array.IndexOf(keep, name) == -1)
                {
                    try
                    {
                        key.DeleteValue(name);
                    }
                    catch
                    {
                    }
                }
            }
            key.Close();
        }

        private void FluentWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_dWindows != null)
            {
                _dWindows.Close();
            }
        }
        private void DoubleClickToHideSwitch_Click(object sender, RoutedEventArgs e)
        {
            _controller.reg.WriteToRegistryRoot("DoubleClickToHide", DoubleClickToHideSwitch.IsChecked!);
            _window.DoubleClickToHide = (bool)DoubleClickToHideSwitch.IsChecked!;
        }

        private void KofiButtonImage_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                ProcessStartInfo sInfo = new ProcessStartInfo($"https://ko-fi.com/J3J61PAH6H") { UseShellExecute = true };
                _ = Process.Start(sInfo);
            }
            catch
            {
            }
        }

        private void ReloadAllFramesButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadAllFramesButton.IsEnabled = false;
            _window.Dispatcher.Invoke(() =>
            {
                _window.TrayIcon.Register();
                if (!_controller.isInitializingInstances)
                    _controller.CheckFrameWindowsLive(true);
            });
            ReloadAllFramesButton.IsEnabled = true;
        }

        private void DragWinBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                try
                {
                    ProcessStartInfo sInfo = new ProcessStartInfo($"https://github.com/PinchToDebug/DragWin") { UseShellExecute = true };
                    _ = Process.Start(sInfo);
                }
                catch
                {
                }
            }
        }
    }
}
