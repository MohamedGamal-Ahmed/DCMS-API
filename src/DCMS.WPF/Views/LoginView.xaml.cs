using System.Windows;
using System.Windows.Controls;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class LoginView : Window
{
    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Password = ((PasswordBox)sender).Password;
            viewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(viewModel.IsLoading))
                {
                    progressBar.Visibility = viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                }
            };
        }
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            if (viewModel.LoginCommand.CanExecute(null))
            {
                viewModel.LoginCommand.Execute(null);
            }
            else
            {
                MessageBox.Show("الرجاء إدخال اسم المستخدم وكلمة المرور", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
