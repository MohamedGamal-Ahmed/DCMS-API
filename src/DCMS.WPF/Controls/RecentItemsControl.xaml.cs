using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DCMS.WPF.Services;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Controls;

public partial class RecentItemsControl : UserControl
{
    public RecentItemsControl()
    {
        InitializeComponent();
    }

    private void OnItemClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListViewItem item && item.Content is RecentItem recentItem)
        {
            if (DataContext is RecentItemsViewModel vm)
            {
                vm.SelectItem(recentItem);
            }
        }
    }
}
