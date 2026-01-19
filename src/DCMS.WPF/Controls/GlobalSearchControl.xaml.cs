using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Controls;

public partial class GlobalSearchControl : UserControl
{
    public GlobalSearchControl()
    {
        InitializeComponent();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is GlobalSearchViewModel vm)
        {
            vm.SearchText = txtSearch.Text;
        }
    }

    private void OnResultClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is GlobalSearchResultItem item)
        {
            if (DataContext is GlobalSearchViewModel vm)
            {
                vm.SelectItem(item);
            }
        }
    }
}
