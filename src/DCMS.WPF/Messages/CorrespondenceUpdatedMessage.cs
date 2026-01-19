using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DCMS.WPF.Messages;

public class CorrespondenceUpdatedMessage : ValueChangedMessage<int>
{
    public CorrespondenceUpdatedMessage(int inboundId) : base(inboundId)
    {
    }
}
