using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DCMS.WPF.Views
{
    public class ConnectionStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Brushes.LimeGreen : Brushes.OrangeRed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ConnectionStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "متصل" : "غير متصل";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToChatColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Blue for my messages, Light Gray for others
            return (bool)value 
                ? (SolidColorBrush)new BrushConverter().ConvertFrom("#0D6EFD")! 
                : (SolidColorBrush)new BrushConverter().ConvertFrom("#F5F5F5")!;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToHeaderColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Brushes.DodgerBlue : Brushes.SeaGreen;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class RoleToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var role = value?.ToString()?.ToLower();
            return role == "user" ? "أنت (You)" : "المساعد الذكي (AI)";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts a username to 1-2 character initials (e.g., "Mohamed Gamal" -> "MG")
    /// </summary>
    public class UserInitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string username && !string.IsNullOrWhiteSpace(username))
            {
                var parts = username.Trim().Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                }
                else if (parts.Length == 1 && parts[0].Length >= 2)
                {
                    return parts[0].Substring(0, 2).ToUpper();
                }
                else if (parts.Length == 1)
                {
                    return parts[0][0].ToString().ToUpper();
                }
            }
            return "?";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Generates a consistent background color for an avatar based on the username's hash.
    /// </summary>
    public class InitialsToColorConverter : IValueConverter
    {
        private static readonly string[] AvatarColors = new[]
        {
            "#3498DB", "#E74C3C", "#2ECC71", "#9B59B6", "#F39C12",
            "#1ABC9C", "#E91E63", "#FF5722", "#607D8B", "#795548"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string username && !string.IsNullOrWhiteSpace(username))
            {
                int hash = Math.Abs(username.GetHashCode());
                int index = hash % AvatarColors.Length;
                return new BrushConverter().ConvertFrom(AvatarColors[index]) as SolidColorBrush;
            }
            return Brushes.Gray;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts an integer to Visibility (Visible if > 0, Collapsed otherwise)
    /// </summary>
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count && count > 0)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
