using System.Windows.Controls;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class ReportingView : UserControl
{
    public ReportingView(ReportingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
