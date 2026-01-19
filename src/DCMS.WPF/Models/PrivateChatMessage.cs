namespace DCMS.WPF.Models;

public class PrivateChatMessage
{
    public string Sender { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsMe { get; set; }
}
