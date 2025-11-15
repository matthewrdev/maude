namespace Maude;

public class MaudeMetricsUpdatedEventArgs : EventArgs
{
    public MaudeMetricsUpdatedEventArgs(IReadOnlyList<MaudeMetric> added, IReadOnlyList<MaudeMetric> removed)
    {
        Added = added ?? throw new ArgumentNullException(nameof(added));
        Removed = removed ?? throw new ArgumentNullException(nameof(removed));
    }

    public IReadOnlyList<MaudeMetric> Added { get; }
    
    public IReadOnlyList<MaudeMetric> Removed { get; }
}