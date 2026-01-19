using System.Windows.Controls;
using System.Windows.Threading;

namespace DCMS.WPF.Views;

public partial class LockScreenView : UserControl
{
    private DispatcherTimer? _clockTimer;

    public LockScreenView()
    {
        InitializeComponent();
        StartClock();
    }

    private void StartClock()
    {
        UpdateClock(); // Initial update
        
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (s, e) => UpdateClock();
        _clockTimer.Start();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        txtTime.Text = now.ToString("hh:mm tt");
        txtDate.Text = now.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("ar-EG"));
    }
}
