using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DCMS.WPF.Views;

public class BooleanToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isUser = (bool)value;
        string alignment = parameter as string ?? "RightLeft"; // Default: User Right, AI Left

        if (alignment == "RightLeft")
        {
            return isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }
        else
        {
            return isUser ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
