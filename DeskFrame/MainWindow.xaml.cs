using System.ComponentModel;
using System.Diagnostics;
using System.Security.Policy;
using System.Windows;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;

namespace DeskFrame
{
    public partial class MainWindow : Window
    {
        private string url = "https://api.github.com/repos/PinchToDebug/DeskFrame/releases/latest";
        bool startOnLogin;
        public static InstanceController _controller;
        public MainWindow()
        {
            InitializeComponent();

            versionHeader.Header += " " + Process.GetCurrentProcess().MainModule!.FileVersionInfo.FileVersion!.ToString();
            _controller = new InstanceController();
            _controller.InitInstances();
            if (_controller.reg.KeyExistsRoot("startOnLogin")) startOnLogin = (bool)_controller.reg.ReadKeyValueRoot("startOnLogin");
            AutorunToggle.IsChecked = startOnLogin;
            if (_controller.reg.KeyExistsRoot("blurBackground")) BlurToggle.IsChecked = (bool)_controller.reg.ReadKeyValueRoot("blurBackground");
            if (_controller.reg.KeyExistsRoot("AutoUpdate") && (bool)_controller.reg.ReadKeyValueRoot("AutoUpdate"))
            {
                Update();
                Debug.WriteLine("Auto update checking for update");
            }
            else if (!_controller.reg.KeyExistsRoot("AutoUpdate"))
            {
                _controller.reg.WriteToRegistryRoot("AutoUpdate", "False");
            }
        }
        private async void Update()
        {
            await Updater.CheckUpdateAsync(url,false);
        }

        private void addDesktopFrame_Click(object sender, RoutedEventArgs e)
        {
            _controller.AddInstance();
        }
        private void Window_Initialized(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;
            this.Width = 0;
            this.Height = 0;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStyle = WindowStyle.None;
            this.Visibility = Visibility.Collapsed;
            this.Left = -500;
            this.Top = -500;

            CloseHide();
        }
        private void CloseHide()
        {
            Task.Run(() =>
            {
                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Close();
                });
            });
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = true;
            this.Hide();
        }

        private async void Update_Button_Click(object sender, RoutedEventArgs e)
        {
            await Updater.CheckUpdateAsync(url,true);

        }
        private void BlurToggle_CheckChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            _controller.reg.WriteToRegistryRoot("blurBackground", BlurToggle.IsChecked!);
            _controller.ChangeBlur((bool)BlurToggle.IsChecked!);
        }
        private void AutorunToggle_CheckChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)AutorunToggle.IsChecked!)
            {

                _controller.reg.AddToAutoRun("DeskFrame", Process.GetCurrentProcess().MainModule!.FileName);
            }
            else
            {
                _controller.reg.RemoveFromAutoRun("DeskFrame");
            }
            _controller.reg.WriteToRegistryRoot("startOnLogin", AutorunToggle.IsChecked);
        }
        private void visitGithub_Buton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo sInfo = new ProcessStartInfo($"https://github.com/PinchToDebug/DeskFrame") { UseShellExecute = true };
                _ = Process.Start(sInfo);
            }
            catch
            {
            }
        }

        private void ExitApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Settings_Button_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow(_controller).Show();
        }
    }
}