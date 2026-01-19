using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Globalization;

namespace DCMS.WPF.Views;

public partial class AiChatView : UserControl
{
    private readonly ViewModels.AiChatViewModel _viewModel;

    public AiChatView(ViewModels.AiChatViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }
}

public class NullToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SmartMentionTextBlock : TextBlock
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(SmartMentionTextBlock), 
            new PropertyMetadata(string.Empty, OnTextChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SmartMentionTextBlock control && e.NewValue is string text)
        {
            control.Inlines.Clear();
            var viewModel = control.DataContext as ViewModels.AiChatViewModel;
            
            // Note: We need a way to trigger navigation. We'll use the OpenRecordCommand from viewModel.
            var inlines = Helpers.ChatMessageParser.ParseMessage(text, 
            (code) => 
            {
                if (viewModel?.OpenRecordCommand.CanExecute(code) == true)
                {
                    viewModel.OpenRecordCommand.Execute($"record://{code.ToLower()}");
                }
            },
            (user) => 
            {
                if (viewModel != null)
                {
                    viewModel.SelectedUser = user;
                }
            });

            foreach (var inline in inlines)
            {
                control.Inlines.Add(inline);
            }
        }
    }
}
