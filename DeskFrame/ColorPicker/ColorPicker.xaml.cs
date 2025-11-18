using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Extensions;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace DeskFrame.ColorPicker
{
    public partial class ColorPicker : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        Point lastMousePoint;
        private byte alpha = 255;
        private bool _isMouseDown = false;
        private bool _isOnColorSelectRect = false;
        private bool _isOnColorStrip = false;
        private bool _isOnOpacityStrip = false;
        private bool _validColor = true;
        public Color ThisColor;
        private Color _opacityColorZero;
        private Color _opacityColorFifty;
        public Color _resultColor;
        private TextBox _tb;

        public Color ResultColor
        {
            get => _resultColor;
            set
            {
                if (_resultColor != value)
                {

                    _resultColor = value;
                    OnPropertyChanged(nameof(ResultColor));
                }
            }
        }
        public Color OpacityColorZero
        {
            get => _opacityColorZero;
            set
            {
                if (_opacityColorZero != value)
                {
                    _opacityColorZero = value;
                    OnPropertyChanged(nameof(OpacityColorZero));
                }
            }
        }
        public Color OpacityColorFifty
        {
            get => _opacityColorFifty;
            set
            {
                if (_opacityColorFifty != value)
                {
                    _opacityColorFifty = value;
                    OnPropertyChanged(nameof(OpacityColorFifty));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (propertyName == "ResultColor")
            {
                _tb.Text = ResultColor.ToString();
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ColorPicker(TextBox tb)
        {
            _tb = tb;
            InitializeComponent();
            this.DataContext = this;
            try
            {
                ThisColor = (Color)ColorConverter.ConvertFromString(_tb.Text);
            }
            catch
            {
                _validColor = false;
            }
            Loaded += ColorWheel_Loaded;
        }
        private void ColorWheel_Loaded(object sender, RoutedEventArgs e)
        {
            var hwndSource = (HwndSource)HwndSource.FromVisual(this);
            hwndSource.AddHook(WndProc);
            if (!_validColor)
            {
                UpdateColorFromStrip((int)(ColorStrip.Width / 2));
                UpdateColorFromRect(ColorSelectRect, new Point(220, 30));
                UpdateColorFromOpacity(0);
            }
            else
            {
                System.Drawing.Color color = System.Drawing.Color.FromArgb(ThisColor.A, ThisColor.R, ThisColor.G, ThisColor.B);
                int x = (int)(ColorSelectRect.ActualWidth * ThisColor.ToHsv().Saturation / 100);
                int y = (int)(ColorSelectRect.ActualHeight - (ColorSelectRect.ActualHeight * ThisColor.ToHsv().Value / 100));
                alpha = ThisColor.A;
                UpdateColorFromOpacity(255 - alpha);
                UpdateColorFromStrip((int)((color.GetHue() / 360) * ColorStrip.ActualWidth));
                UpdateColorFromRect(ColorSelectRect, new Point(x, y));
                Debug.WriteLine($"alpha:  {color.A}\nalpha2: {ThisColor.A}\nx: {x}\ny: {y}\nh: {color.GetHue()}");
            }
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0201) // WM_LBUTTONDOWN
            {
                HandleMouseDown();
            }
            if (msg == 0x0200) // WM_MOUSEMOVE
            {
                HandleMouseMove();
            }
            if (msg == 0x0202) // WM_LBUTTONUP
            {
                _isMouseDown = false;
            }
            return IntPtr.Zero;
        }
        private void HandleMouseDown()
        {
            if (ColorStrip.IsMouseOver) _isOnColorStrip = true;
            else _isOnColorStrip = false;

            if (ColorSelectRect.IsMouseOver) _isOnColorSelectRect = true;
            else _isOnColorSelectRect = false;

            if (OpacityStrip.IsMouseOver) _isOnOpacityStrip = true;
            else _isOnOpacityStrip = false;

            if (_isOnColorStrip)
            {
                _isMouseDown = true;
                UpdateColorFromStrip((int)Mouse.GetPosition(ColorStrip).X);
            }
            else if (_isOnColorSelectRect)
            {
                _isMouseDown = true;
                UpdateColorFromRect(ColorSelectRect, Mouse.GetPosition(ColorSelectRect));
            }
            else if (_isOnOpacityStrip)
            {
                _isMouseDown = true;
                UpdateColorFromOpacity(Mouse.GetPosition(OpacityStrip).X);
            }
        }
        private void HandleMouseMove()
        {
            if (_isMouseDown)
            {
                if (_isOnColorStrip)
                {
                    UpdateColorFromStrip((int)Mouse.GetPosition(ColorStrip).X);
                }
                else if (_isOnColorSelectRect)
                {
                    UpdateColorFromRect(ColorSelectRect, Mouse.GetPosition(ColorSelectRect));
                }
                else if (_isOnOpacityStrip)
                {
                    UpdateColorFromOpacity(Mouse.GetPosition(OpacityStrip).X);
                }
            }
        }
        private void UpdateColorFromRect(Rectangle element, Point mousePosition)
        {
            int x = (int)mousePosition.X;
            int y = (int)mousePosition.Y;

            if (x < 0) x = 0;
            if (x >= element.RenderSize.Width) x = (int)element.RenderSize.Width - 1;
            if (y < 0) y = 0;
            if (y >= element.RenderSize.Height) y = (int)element.RenderSize.Height - 1;

            RenderTargetBitmap renderTarget = new RenderTargetBitmap(
                (int)element.RenderSize.Width,
                (int)element.RenderSize.Height, 96, 96,
                PixelFormats.Pbgra32);

            renderTarget.Render(element);

            int pitch = renderTarget.PixelWidth * 4;
            byte[] pixels = new byte[renderTarget.PixelHeight * pitch];
            renderTarget.CopyPixels(pixels, pitch, 0);

            int pixelIndex = Math.Clamp((y * pitch) + (x * 4), 0, pixels.Length - 4);

            Canvas.SetLeft(RectSelector, x - RectSelector.Width / 2);
            Canvas.SetTop(RectSelector, y - RectSelector.Height / 2);

            byte blue = pixels[pixelIndex];
            byte green = pixels[pixelIndex + 1];
            byte red = pixels[pixelIndex + 2];

            RectSelector.Fill = new SolidColorBrush(Color.FromArgb(255, red, green, blue));
            ResultColor = Color.FromArgb(alpha, red, green, blue);
            OpacityColorZero = Color.FromArgb(255, red, green, blue);
            OpacityColorFifty = Color.FromArgb(127, red, green, blue);

            lastMousePoint = mousePosition;
        }
        private void UpdateColorFromStrip(double mouseX)
        {
            double normalizedX = Math.Min(Math.Max(mouseX, 0), ColorStrip.Width);

            Canvas.SetLeft(HueSelector, normalizedX - HueSelector.Width / 2);
            Canvas.SetTop(HueSelector, Canvas.GetTop(ColorStrip) + (ColorStrip.ActualHeight - HueSelector.ActualHeight) / 2);
            var gradientBrush = (LinearGradientBrush)((Rectangle)ColorCanvas.Children[0]).Fill;
            var color = GetColorFromGradient(gradientBrush, normalizedX / ColorStrip.Width);
            SelectedColor.Color = color;
            Color a = color;
            a.A = 127;
            HueSelector.Fill = new SolidColorBrush(color);
            UpdateColorFromRect(ColorSelectRect, lastMousePoint);
        }
        private void UpdateColorFromOpacity(double mouseX)
        {
            double normalizedX = Math.Min(Math.Max(mouseX, 0), OpacityStrip.Width);
            Canvas.SetLeft(OpacitySelector, normalizedX - OpacitySelector.ActualWidth / 2);
            Canvas.SetTop(OpacitySelector, Canvas.GetTop(OpacityStrip) + (OpacityStrip.ActualHeight - OpacitySelector.ActualHeight) / 2);

            OpacityPercentageTB.Text = "Opacity: " + (100 - (int)(normalizedX / 256 * 100)).ToString() + "%";

            _opacityColorZero.A = (byte)Math.Abs(255 - normalizedX);
            alpha = _opacityColorZero.A;
            ResultColor = _opacityColorZero;
        }

        private Color GetColorFromGradient(LinearGradientBrush gradientBrush, double offset)
        {
            for (int i = 0; i < gradientBrush.GradientStops.Count - 1; i++)
            {
                var stop1 = gradientBrush.GradientStops[i];
                var stop2 = gradientBrush.GradientStops[i + 1];

                if (offset >= stop1.Offset && offset <= stop2.Offset)
                {
                    double progress = (offset - stop1.Offset) / (stop2.Offset - stop1.Offset);
                    byte r = (byte)(stop1.Color.R + (stop2.Color.R - stop1.Color.R) * progress);
                    byte g = (byte)(stop1.Color.G + (stop2.Color.G - stop1.Color.G) * progress);
                    byte b = (byte)(stop1.Color.B + (stop2.Color.B - stop1.Color.B) * progress);

                    return Color.FromRgb(r, g, b);
                }
            }
            return Colors.Black;
        }
    }
}