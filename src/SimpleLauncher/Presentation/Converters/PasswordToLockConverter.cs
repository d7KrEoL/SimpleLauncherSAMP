using System.Globalization;
using System.Windows.Data;

namespace SimpleLauncher.Presentation.Converters
{
    public class PasswordToLockConverter : IValueConverter
    {
        // Юникод: 🔒 (закрытый замок), 🔓 (открытый замок)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasPassword = value is bool b && b;
            return hasPassword ? "🔒" : string.Empty; // "🔓";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
