using System.Windows.Threading;
using System.Windows.Input;
using System;

namespace DCMS.WPF.Services;

public class IdleDetectorService
{
    private readonly DispatcherTimer _timer;
    private DateTime _lastActivity;
    private bool _isActive;

    // Timeout duration (15 minutes for better UX)
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(15);

    public event EventHandler? IdleDetected;

    public IdleDetectorService()
    {
        _timer = new DispatcherTimer();
        _timer.Interval = TimeSpan.FromSeconds(10); // Check every 10 seconds
        _timer.Tick += OnTimerTick;
    }

    public void Start()
    {
        if (_isActive) return;

        InputManager.Current.PreProcessInput += OnInputPreProcess;
        _lastActivity = DateTime.Now;
        _timer.Start();
        _isActive = true;
    }

    public void Stop()
    {
        if (!_isActive) return;

        InputManager.Current.PreProcessInput -= OnInputPreProcess;
        _timer.Stop();
        _isActive = false;
    }

    private void OnInputPreProcess(object sender, PreProcessInputEventArgs e)
    {
        var input = e.StagingItem.Input;

        if (input is MouseEventArgs || input is KeyboardEventArgs || input is TextCompositionEventArgs)
        {
             _lastActivity = DateTime.Now;
        }
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var idleTime = DateTime.Now - _lastActivity;
        if (idleTime >= _timeout)
        {
            Stop(); // Stop monitoring once idle is detected
            IdleDetected?.Invoke(this, EventArgs.Empty);
        }
    }
}
