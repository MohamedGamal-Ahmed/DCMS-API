using System.Windows.Controls;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class EmailInboundView : UserControl
{
    public EmailInboundView()
    {
        InitializeComponent();
    }

    public EmailInboundView(EmailInboundViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
