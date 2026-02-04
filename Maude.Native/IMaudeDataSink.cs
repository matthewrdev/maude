namespace Maude;

/// <summary>
/// Contract for storing and querying Maude's metrics/events and notifying listeners about updates.
/// </summary>
public interface IMaudeDataSink
{
    /// <summary>
    /// All channels currently tracked by the sink.
    /// </summary>
    public IReadOnlyList<MaudeChannel> Channels { get; }
    
    /// <summary>
    /// Occurs when a new set of metrics is added to the data sink or existing values are removed or both.
    /// </summary>
    public event EventHandler<MaudeMetricsUpdatedEventArgs>? OnMetricsUpdated;
    
    /// <summary>
    /// Occurs when a new set of events are added or existing values are removed or both.
    /// </summary>
    public event EventHandler<MaudeEventsUpdatedEventArgs>? OnEventsUpdated;
    
    /// <summary>
    /// Returns all metrics across all channels.
    /// </summary>
    IReadOnlyList<MaudeMetric> Metrics { get; }
    
    /// <summary>
    /// Returns all events across all channels.
    /// </summary>
    IReadOnlyList<MaudeEvent> Events { get; }
    
    /// <summary>
    /// Gets all metrics for the given channel.
    /// </summary>
    IReadOnlyList<MaudeMetric> GetMetricsForChannel(MaudeChannel channel);
    
    /// <summary>
    /// Gets all metrics for the given channel ID.
    /// </summary>
    IReadOnlyList<MaudeMetric> GetMetricsForChannel(byte channelId);
    
    /// <summary>
    /// Gets metrics for a channel within a UTC time window.
    /// </summary>
    IReadOnlyList<MaudeMetric> GetMetricsForChannelInRange(MaudeChannel channel, DateTime fromUtc, DateTime toUtc);
    
    /// <summary>
    /// Gets metrics for a channel ID within a UTC time window.
    /// </summary>
    IReadOnlyList<MaudeMetric> GetMetricsForChannelInRange(byte channelId, DateTime fromUtc, DateTime toUtc);
    
    /// <summary>
    /// Returns a span descriptor for metrics in a given range.
    /// </summary>
    MaudeChannelSpan GetMetricsChannelSpanForRange(byte channelId, DateTime fromUtc, DateTime toUtc);
    
    /// <summary>
    /// Executes a consumer against metrics in a given range without extra allocations.
    /// </summary>
    void UseMetricsInChannelForRange(byte channelId, DateTime fromUtc, DateTime toUtc, Action<ReadOnlySpan<MaudeMetric>> useAction);
    
    /// <summary>
    /// Gets all events for the given channel.
    /// </summary>
    IReadOnlyList<MaudeEvent> GetEventsForChannel(MaudeChannel channel);
    
    /// <summary>
    /// Gets all events for the given channel ID.
    /// </summary>
    IReadOnlyList<MaudeEvent> GetEventsForChannel(byte channelId);
    
    /// <summary>
    /// Gets events for a channel within a UTC time window.
    /// </summary>
    IReadOnlyList<MaudeEvent> GetEventsForChannelInRange(MaudeChannel channel, DateTime fromUtc, DateTime toUtc);
    
    /// <summary>
    /// Gets events for a channel ID within a UTC time window.
    /// </summary>
    IReadOnlyList<MaudeEvent> GetEventsForChannelInRange(byte channelId, DateTime fromUtc, DateTime toUtc);
    
    /// <summary>
    /// Executes a consumer against events in a given range without extra allocations.
    /// </summary>
    void UseEventsInChannelForRange(byte channelId, DateTime fromUtc, DateTime toUtc, Action<ReadOnlySpan<MaudeEvent>> useAction);

    /// <summary>
    /// Records an event on the unspecified channel using the default event type.
    /// </summary>
    void Event(string label);

    /// <summary>
    /// Records an event on the unspecified channel with a specific event type.
    /// </summary>
    void Event(string label, MaudeEventType type);

    /// <summary>
    /// Records an event on the unspecified channel with a specific event type and details.
    /// </summary>
    void Event(string label, MaudeEventType type, string details);

    /// <summary>
    /// Records an event for the given channel using the default event type.
    /// </summary>
    void Event(string label, byte channel);

    /// <summary>
    /// Records an event for the given channel with a specific event type.
    /// </summary>
    void Event(string label, MaudeEventType type, byte channel);

    /// <summary>
    /// Records an event for the given channel with a specific event type and details.
    /// </summary>
    void Event(string label, MaudeEventType type, byte channel, string details);

    /// <summary>
    /// Creates a full copy of the data currently in the data sink for export.
    /// </summary>
    MaudeSnapshot Snapshot();
}
