using System.Windows.Data;

namespace DeskFrame
{
    public class IconSizeToItemHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int iconSize)
            {
                return iconSize + 40 + iconSize / 4;
            }
            return 72; // 32 + 40
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}