using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace DeskFrame
{
    public class BooleanFrameTypeToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isShortcutsOnly)
            {
                return isShortcutsOnly ? SymbolRegular.Document16 : SymbolRegular.Folder16;
            }
            return SymbolRegular.Folder16;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}