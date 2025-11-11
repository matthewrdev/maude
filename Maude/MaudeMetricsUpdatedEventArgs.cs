namespace Maude;

public class MaudeMetricsUpdatedEventArgs : EventArgs
{
    public MaudeMetricsUpdatedEventArgs(IReadOnlyList<MaudeEvent> added, IReadOnlyList<MaudeEvent> removed, byte channel)
    {
        Channel = channel;
        Added = added ?? throw new ArgumentNullException(nameof(added));
        Removed = removed ?? throw new ArgumentNullException(nameof(removed));
    }

    public byte Channel { get; }
    
    public IReadOnlyList<MaudeEvent> Added { get; }
    
    public IReadOnlyList<MaudeEvent> Removed { get; }
}