using System.Windows;

namespace DCMS.WPF.Views;

public partial class AuditLogDetailsDialog : Window
{
    public AuditLogDetailsDialog()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
