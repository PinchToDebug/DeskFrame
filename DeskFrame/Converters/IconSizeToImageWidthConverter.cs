using System.Windows.Data;

namespace DeskFrame
{
    public class IconSizeToImageWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int iconSize)
            {
                return iconSize;
            }
            return 32; // Default size
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}