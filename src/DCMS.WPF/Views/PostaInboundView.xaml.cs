using System.Windows.Controls;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class PostaInboundView : UserControl
{
    public PostaInboundView()
    {
        InitializeComponent();
    }

    public PostaInboundView(PostaInboundViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
