namespace Dobropay.Notifications.Persistence;

public sealed class LocalReceiverEvent
{
    public Guid EventId { get; set; }
    public int Attempts { get; set; }
    public bool Verified { get; set; }
    public string? Body { get; set; }
}
