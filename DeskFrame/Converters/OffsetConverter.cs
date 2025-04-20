using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DeskFrame
{
    public class OffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double input)
            {
                if (input <= 110)
                {
                    return input - 100;
                }
                return 10;
            }
            return 80;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}