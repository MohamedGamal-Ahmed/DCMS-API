using System.Windows.Controls;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class UserManagementView : UserControl
{
    public UserManagementView(UserManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
