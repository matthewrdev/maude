namespace Maude.Runtime;

public class MaudeEvent
{
    private readonly int? channel;

    public MaudeEvent(string label, string icon, DateTime capturedAtUtc, object externalId, int? channel)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        this.channel = channel;

        Label = label;
        Icon = icon;
        CapturedAtUtc = capturedAtUtc;
        ExternalId = externalId;
        Id = Guid.CreateVersion7();
    }

    public string Label {get;}
    
    public string Icon {get;}
    
    public DateTime CapturedAtUtc {get; }
    
    public Guid Id {get;}
    
    public object ExternalId {get;}
    
    /// <summary>
    /// The <see cref="MaudeChannel"/> that this event is connected to.
    /// </summary>
    public int? Channel {get;}
    
}