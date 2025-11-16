namespace Maude;

/// <summary>
/// Provides details about events added to or removed from the data sink.
/// </summary>
public class MaudeEventsUpdatedEventArgs : EventArgs
{
    public MaudeEventsUpdatedEventArgs(IReadOnlyList<MaudeEvent> added, IReadOnlyList<MaudeEvent> removed)
    {
        Added = added ?? throw new ArgumentNullException(nameof(added));
        Removed = removed ?? throw new ArgumentNullException(nameof(removed));
    }

    /// <summary>
    /// Events that have been appended since the last update.
    /// </summary>
    public IReadOnlyList<MaudeEvent> Added { get; }
    
    /// <summary>
    /// Events that have been pruned since the last update.
    /// </summary>
    public IReadOnlyList<MaudeEvent> Removed { get; }
}
