namespace Maude;

/// <summary>
/// Provides details about metric samples added to or removed from the data sink.
/// </summary>
public class MaudeMetricsUpdatedEventArgs : EventArgs
{
    public MaudeMetricsUpdatedEventArgs(IReadOnlyList<MaudeMetric> added, IReadOnlyList<MaudeMetric> removed)
    {
        Added = added ?? throw new ArgumentNullException(nameof(added));
        Removed = removed ?? throw new ArgumentNullException(nameof(removed));
    }

    /// <summary>
    /// Metric samples appended since the last update.
    /// </summary>
    public IReadOnlyList<MaudeMetric> Added { get; }
    
    /// <summary>
    /// Metric samples pruned since the last update.
    /// </summary>
    public IReadOnlyList<MaudeMetric> Removed { get; }
}
