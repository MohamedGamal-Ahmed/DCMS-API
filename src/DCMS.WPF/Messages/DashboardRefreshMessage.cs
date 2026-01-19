using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DCMS.WPF.Messages;

/// <summary>
/// Message sent when an inbound item is saved, updated, or its status changes.
/// The dashboard should refresh to reflect the changes.
/// </summary>
public class DashboardRefreshMessage : ValueChangedMessage<int>
{
    public DashboardRefreshMessage(int inboundId) : base(inboundId) { }
}
