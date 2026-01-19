using System.Windows;

namespace DCMS.WPF.Views.Dialogs;

public partial class CodeEditorDialog : Window
{
    public CodeEditorDialog()
    {
        InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
