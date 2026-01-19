using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DCMS.WPF.Converters;

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isVisible = false;

        if (value is bool b)
        {
            isVisible = b;
        }
        else if (value is int i)
        {
            isVisible = i > 0;
        }
        else if (value != null)
        {
            isVisible = true;
        }

        // Inverse: If TRUE (has items), return Collapsed. If FALSE (empty), return Visible.
        return isVisible ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
