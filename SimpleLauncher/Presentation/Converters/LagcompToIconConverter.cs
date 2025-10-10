using System.Globalization;
using System.Windows.Data;

namespace SimpleLauncher.Presentation.Converters
{
    public class LagcompToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isLagcomp = value is bool b && b;
            return isLagcomp ? "⚡" : "⏱️";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
