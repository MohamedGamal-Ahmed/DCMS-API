using System.Windows.Controls;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class InboundTypeSelectorView : UserControl
{
    public InboundTypeSelectorView(InboundTypeSelectorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Subscribe to navigation event
        viewModel.NavigateToForm += OnNavigateToForm;
    }

    public event EventHandler<UserControl>? NavigateToForm;

    private void OnNavigateToForm(object? sender, UserControl control)
    {
        NavigateToForm?.Invoke(this, control);
    }
}
