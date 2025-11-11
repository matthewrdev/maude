namespace Maude;

public struct MaudeEvent
{
    public MaudeEvent(string label, string icon, DateTime capturedAtUtc, object externalId, int? channel)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));

        Label = label;
        Icon = icon;
        CapturedAtUtc = capturedAtUtc;
        ExternalId = externalId;
        Id = Guid.CreateVersion7();
    }

    public string Label {get;}
    
    public string Icon {get;}
    
    public DateTime CapturedAtUtc {get; }
    
    /// <summary>
    /// The Maude ID of this event.
    /// </summary>
    public Guid Id {get;}
    
    /// <summary>
    /// An optional, typeless 
    /// </summary>
    public object ExternalId { get;}
    
    /// <summary>
    /// The <see cref="MaudeChannel"/> that this event is connected to.
    /// <para/>
    /// If null, this is a global event
    /// </summary>
    public int? Channel {get;}
    
}