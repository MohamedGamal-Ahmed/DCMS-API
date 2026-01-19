using CommunityToolkit.Mvvm.Messaging;
using System;

namespace DCMS.WPF.Services;

/// <summary>
/// Service to manage database polling suspension during idle/lock states.
/// This helps save Neon CU-hrs by stopping background queries when the app is locked.
/// </summary>
public class DatabasePollingService
{
    private bool _isSuspended;
    
    public bool IsSuspended => _isSuspended;
    
    public event EventHandler? PollingResumed;
    public event EventHandler? PollingSuspended;

    /// <summary>
    /// Suspends all database polling. Called when Lock Screen is shown.
    /// </summary>
    public void Suspend()
    {
        if (_isSuspended) return;
        _isSuspended = true;
        PollingSuspended?.Invoke(this, EventArgs.Empty);
        System.Diagnostics.Debug.WriteLine("[DB POLLING] Suspended - Saving CU-hrs");
    }

    /// <summary>
    /// Resumes database polling. Called when user unlocks the app.
    /// </summary>
    public void Resume()
    {
        if (!_isSuspended) return;
        _isSuspended = false;
        PollingResumed?.Invoke(this, EventArgs.Empty);
        System.Diagnostics.Debug.WriteLine("[DB POLLING] Resumed");
    }
}
