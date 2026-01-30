using System.Windows.Controls;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class RequestInboundView : UserControl
{
    public RequestInboundView()
    {
        InitializeComponent();
    }

    public RequestInboundView(RequestInboundViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
