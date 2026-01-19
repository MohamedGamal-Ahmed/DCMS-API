using System;
using System.Globalization;
using System.Text.Json;
using System.Windows.Data;

namespace DCMS.WPF.Converters
{
    public class JsonFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string json && !string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    using var document = JsonDocument.Parse(json);
                    return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
                }
                catch
                {
                    return json;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
