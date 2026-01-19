using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DCMS.WPF.Services;

public class NotificationTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ToastType type)
        {
            return type switch
            {
                ToastType.Success => Brushes.Green,
                ToastType.Error => Brushes.Red,
                ToastType.Warning => Brushes.Orange,
                ToastType.Info => Brushes.DeepSkyBlue,
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class NotificationTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ToastType type)
        {
            return type switch
            {
                ToastType.Success => "✅",
                ToastType.Error => "❌",
                ToastType.Warning => "⚠️",
                ToastType.Info => "ℹ️",
                _ => "🔔"
            };
        }
        return "🔔";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
