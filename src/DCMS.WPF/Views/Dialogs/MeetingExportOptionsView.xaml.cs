using System.Windows;
using DCMS.WPF.ViewModels.Dialogs;

namespace DCMS.WPF.Views.Dialogs;

public partial class MeetingExportOptionsView : Window
{
    public MeetingExportOptionsView(MeetingExportOptionsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += () => Close();
    }
}
