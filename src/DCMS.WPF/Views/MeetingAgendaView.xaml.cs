using System.Windows.Controls;
using DCMS.WPF.ViewModels;

namespace DCMS.WPF.Views;

public partial class MeetingAgendaView : UserControl
{
    public MeetingAgendaView(MeetingAgendaViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void ExportButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu != null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }
}
