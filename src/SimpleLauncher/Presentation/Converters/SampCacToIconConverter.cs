using System.Globalization;
using System.Windows.Data;

namespace SimpleLauncher.Presentation.Converters
{
    public class SampCacToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSampCac = value is bool b && b;
            return isSampCac ? "🛡️" : "➖";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
