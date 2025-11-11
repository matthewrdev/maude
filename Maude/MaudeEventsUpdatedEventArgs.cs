namespace Maude;

public class MaudeEventsUpdatedEventArgs : EventArgs
{
    public MaudeEventsUpdatedEventArgs(IReadOnlyList<MaudeEvent> added, IReadOnlyList<MaudeEvent> removed, byte? channel)
    {
        Added = added ?? throw new ArgumentNullException(nameof(added));
        Removed = removed ?? throw new ArgumentNullException(nameof(removed));
        Channel = channel;
    }

    public IReadOnlyList<MaudeEvent> Added { get; }
    
    public IReadOnlyList<MaudeEvent> Removed { get; }
    public byte? Channel { get; }
}