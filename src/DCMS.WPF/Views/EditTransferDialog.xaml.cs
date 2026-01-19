using DCMS.WPF.ViewModels;
using System.Windows;

namespace DCMS.WPF.Views;

public partial class EditTransferDialog : Window
{
    public EditTransferDialog(EditTransferViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
