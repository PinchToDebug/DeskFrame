using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

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
    }
}