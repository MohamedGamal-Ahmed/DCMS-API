using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Windows.Data;

namespace DCMS.WPF.Converters
{
    public class SmartJsonFormatConverter : IValueConverter
    {
        private static readonly string[] ImportantFields = new[]
        {
            "Id", "SubjectNumber", "Code", "Subject", "Title", "Name",
            "FromEntity", "FromEngineer", "ResponsibleEngineer",
            "Status", "InboundDate", "Reply", "TransferDate", "TransferredTo",
            "Username", "Role", "Email", "IsActive",
            "Description", "StartDateTime", "Location"
        };

        private static readonly Dictionary<string, string> ArabicFieldNames = new()
        {
            { "Id", "المعرف" },
            { "SubjectNumber", "رقم الموضوع" },
            { "Code", "الكود" },
            { "Subject", "الموضوع" },
            { "Title", "العنوان" },
            { "Name", "الاسم" },
            { "FromEntity", "من جهة" },
            { "FromEngineer", "من مهندس" },
            { "ResponsibleEngineer", "المهندس المسئول" },
            { "Status", "الحالة" },
            { "InboundDate", "تاريخ الوارد" },
            { "OutboundDate", "تاريخ الصادر" },
            { "Reply", "الرد" },
            { "TransferDate", "تاريخ التحويل" },
            { "TransferredTo", "محول إلى" },
            { "Username", "اسم المستخدم" },
            { "Role", "الدور" },
            { "Email", "البريد الإلكتروني" },
            { "IsActive", "نشط" },
            { "Description", "الوصف" },
            { "StartDateTime", "تاريخ البداية" },
            { "Location", "الموقع" },
            { "AttachmentUrl", "رابط المرفق" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string json && !string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;

                    var lines = new System.Text.StringBuilder();
                    
                    foreach (var property in root.EnumerateObject())
                    {
                        // Only show important fields
                        if (ImportantFields.Contains(property.Name))
                        {
                            var arabicName = ArabicFieldNames.ContainsKey(property.Name) 
                                ? ArabicFieldNames[property.Name] 
                                : property.Name;
                            
                            var valueStr = property.Value.ValueKind switch
                            {
                                JsonValueKind.String => property.Value.GetString(),
                                JsonValueKind.Number => property.Value.GetDecimal().ToString(),
                                JsonValueKind.True => "✓",
                                JsonValueKind.False => "✗",
                                JsonValueKind.Null => "-",
                                _ => property.Value.ToString()
                            };

                            if (!string.IsNullOrWhiteSpace(valueStr))
                            {
                                lines.AppendLine($"{arabicName}: {valueStr}");
                            }
                        }
                    }

                    return lines.ToString();
                }
                catch
                {
                    return json;
                }
            }
            return value ?? "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
