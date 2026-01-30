using System.Windows.Controls;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class ContractInboundView : UserControl
{
    public ContractInboundView()
    {
        InitializeComponent();
    }

    public ContractInboundView(ContractInboundViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
