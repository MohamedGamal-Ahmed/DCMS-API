using System.Windows;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class AddMeetingDialog : Window
{
    public AddMeetingDialog(AddMeetingDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
