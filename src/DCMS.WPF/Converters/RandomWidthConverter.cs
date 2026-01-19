using System;
using System.Globalization;
using System.Windows.Data;

namespace DCMS.WPF.Converters
{
    public class RandomWidthConverter : IMultiValueConverter
    {
        private readonly Random _random = new Random();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[1] is double totalWidth) || totalWidth <= 0)
                return double.NaN;

            // Generate a random width between 60% and 95% of the total width
            double percentage = 0.6 + (_random.NextDouble() * 0.35);
            return totalWidth * percentage;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
