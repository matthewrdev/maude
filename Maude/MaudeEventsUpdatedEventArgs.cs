namespace Maude;

public class MaudeEventsUpdatedEventArgs : EventArgs
{
    public MaudeEventsUpdatedEventArgs(IReadOnlyList<MaudeEvent> added, IReadOnlyList<MaudeEvent> removed)
    {
        Added = added ?? throw new ArgumentNullException(nameof(added));
        Removed = removed ?? throw new ArgumentNullException(nameof(removed));
    }

    public IReadOnlyList<MaudeEvent> Added { get; }
    
    public IReadOnlyList<MaudeEvent> Removed { get; }
}