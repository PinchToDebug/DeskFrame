using System.Diagnostics;
using System.Reflection;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using TextBlock = System.Windows.Controls.TextBlock;

namespace DeskFrame
{
    public partial class AboutWindow : FluentWindow
    {
        public AboutWindow(double top, double left, double height, double width)
        {
            InitializeComponent();
            this.Width = width / 2;
            this.Left = left + (width - this.Width) / 2;
            this.Top = top + (height - this.Height) / 2;

            var v = Assembly.GetExecutingAssembly().GetName().Version;
            VersionTextBlock.Text = $"Version {v.Major}.{v.Minor}.{v.Build}";
        }

        private void Profile_TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((TextBlock)sender).Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFABB8FF"));
        }

        private void Profile_TextBlock_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((TextBlock)sender).Foreground = System.Windows.Media.Brushes.White;
        }

        private void Profile_TextBlock_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                ProcessStartInfo sInfo = new ProcessStartInfo($"https://github.com/PinchToDebug") { UseShellExecute = true };
                _ = Process.Start(sInfo);
            }
            catch
            {
            }
        }

    }
}