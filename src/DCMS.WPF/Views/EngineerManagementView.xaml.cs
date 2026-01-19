using System.Windows.Controls;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class EngineerManagementView : UserControl
{
    public EngineerManagementView(EngineerManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    // Default constructor
    public EngineerManagementView()
    {
        InitializeComponent();
    }
}
