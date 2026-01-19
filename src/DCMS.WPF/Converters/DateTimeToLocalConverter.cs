using System;
using System.Globalization;
using System.Windows.Data;

namespace DCMS.WPF.Converters;

public class DateTimeToLocalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            // If it's UTC, convert to local. If it's already local, keep it.
            return dateTime.Kind == DateTimeKind.Utc ? dateTime.ToLocalTime() : dateTime;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.ToUniversalTime();
        }
        return value;
    }
}
