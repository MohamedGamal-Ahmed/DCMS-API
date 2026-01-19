using DCMS.WPF.ViewModels;
using System.Windows;

namespace DCMS.WPF.Views;

public partial class EditFollowUpDialog : Window
{
    public EditFollowUpDialog(EditFollowUpViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += () => Close();
    }
}
