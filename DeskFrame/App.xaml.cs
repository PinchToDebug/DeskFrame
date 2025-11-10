using DeskFrame.Properties;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace DeskFrame
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private DispatcherTimer updateTimer;
        public RegistryHelper reg = new RegistryHelper("DeskFrame");
        protected override void OnStartup(StartupEventArgs e)
        {
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {

                var dialog = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "DeskFrame",
                    Content = Lang.DeskFrame_AlreadyRunning,
                };

                var result = dialog.ShowDialogAsync();

                if (result.Result == Wpf.Ui.Controls.MessageBoxResult.None)
                {
                    Application.Current.Shutdown();
                }
            }
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;
            base.OnStartup(e);
            ToastNotificationManagerCompat.OnActivated += ToastActivatedHandler;
            StartUpdateCheckTimer();
        }
        private void ToastActivatedHandler(ToastNotificationActivatedEventArgsCompat toastArgs)
        {
            var args = ToastArguments.Parse(toastArgs.Argument);
            Current.Dispatcher.Invoke(async () =>
            {
                if (args.Contains("action") && args["action"] == "install_update")
                {
                   await Updater.InstallUpdate();
                }

            });
        }
        private void StartUpdateCheckTimer()
        {
            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(6)
            };
            updateTimer.Tick += async (_, _) =>
            {
                if (reg.KeyExistsRoot("AutoUpdate") && (bool)reg.ReadKeyValueRoot("AutoUpdate"))
                {
                    await Updater.CheckUpdateAsync("https://api.github.com/repos/PinchToDebug/DeskFrame/releases/latest",true);
                }
            };
            updateTimer.Start();
        }
    }

}
