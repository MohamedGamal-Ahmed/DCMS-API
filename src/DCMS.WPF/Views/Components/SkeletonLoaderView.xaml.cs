using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace DCMS.WPF.Views.Components;

public partial class SkeletonLoaderView : UserControl
{
    public SkeletonLoaderView()
    {
        InitializeComponent();
        GenerateRows();
    }

    public ObservableCollection<int> Rows { get; } = new();

    public static readonly DependencyProperty RowCountProperty =
        DependencyProperty.Register("RowCount", typeof(int), typeof(SkeletonLoaderView), 
            new PropertyMetadata(8, OnRowCountChanged));

    public int RowCount
    {
        get => (int)GetValue(RowCountProperty);
        set => SetValue(RowCountProperty, value);
    }

    private static void OnRowCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SkeletonLoaderView loader)
        {
            loader.GenerateRows();
        }
    }

    private void GenerateRows()
    {
        Rows.Clear();
        for (int i = 0; i < RowCount; i++)
        {
            Rows.Add(i);
        }
    }
}
