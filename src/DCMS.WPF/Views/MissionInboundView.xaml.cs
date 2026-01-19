using System.Windows.Controls;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class MissionInboundView : UserControl
{
    public MissionInboundView(MissionInboundViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
