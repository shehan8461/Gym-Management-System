using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GymManagementSystem.Converters
{
    public class TypeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string type)
            {
                if (type.Equals("Biometric", StringComparison.OrdinalIgnoreCase))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4caf50")); // Green
                
                if (type.Equals("Manual", StringComparison.OrdinalIgnoreCase))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff9800")); // Orange
            }
            
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#757575")); // Grey default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
