using System.Windows.Controls;
using System.Windows;
using DCMS.Domain.Entities;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class SearchAndFollowUpView : UserControl
{
    public SearchAndFollowUpView(SearchAndFollowUpViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

}
