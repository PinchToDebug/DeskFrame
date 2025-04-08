using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DeskFrame
{
    public class TitleBarBoolToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool enabled)
            {
                return enabled ? 28.7 : 30;
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}