using System.Globalization;
using System.Windows.Data;

namespace DCMS.WPF.Converters;

public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return d * 100;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return d / 100;
        }
        return 0;
    }
}
