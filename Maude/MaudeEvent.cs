namespace Maude;

public class MaudeEvent : IComparable<MaudeEvent>
{
    public MaudeEvent(string label, 
                      string icon,
                      DateTime capturedAtUtc, 
                      object externalId, 
                      byte channel)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));

        Label = label;
        Icon = icon;
        CapturedAtUtc = capturedAtUtc;
        ExternalId = externalId;
        Channel = channel;
        Id = Guid.CreateVersion7();
    }

    public string Label {get;}
    
    /// <summary>
    /// A <see cref="MaterialSymbols"/> icon code.
    /// <para/>
    /// 
    /// </summary>
    public string Icon {get;}
    
    /// <summary>
    /// The date-time that this event was captured, in UTC time.
    /// </summary>
    public DateTime CapturedAtUtc {get; }
    
    /// <summary>
    /// The internal Maude ID of this event.
    /// </summary>
    public Guid Id {get;}
    
    /// <summary>
    /// An optional ID that you may use to identify this event to something within the outer application.
    /// </summary>
    public object ExternalId { get;}
    
    /// <summary>
    /// The <see cref="MaudeChannel"/> that this event is connected to.
    /// </summary>
    public byte Channel {get;}

    public int CompareTo(MaudeEvent? other)
    {
        return other is null ? 1 : DateTime.Compare(CapturedAtUtc,  other.CapturedAtUtc);
    }
}