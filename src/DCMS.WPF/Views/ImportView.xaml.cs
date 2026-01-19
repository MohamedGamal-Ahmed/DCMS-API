using System.Windows.Controls;

namespace DCMS.WPF.Views;

public partial class ImportView : UserControl
{
    public ImportView(ViewModels.ImportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
