using System.Windows.Data;

namespace DeskFrame
{
    public class IconSizeToItemWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int iconSize)
            {
                return iconSize + 20;
            }
            return 85;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}