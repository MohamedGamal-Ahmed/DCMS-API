using System.Windows.Controls;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class PostaOutboundView : UserControl
{
    public PostaOutboundView()
    {
        InitializeComponent();
    }

    public PostaOutboundView(PostaOutboundViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
