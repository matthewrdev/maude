namespace Maude;

/// <summary>
/// The 
/// </summary>
public interface IMaudeDataSink
{
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
    /// 
    /// </summary>
    IReadOnlyList<MaudeEvent> Events { get; }
    
    IReadOnlyList<MaudeMetric> GetMetricsForChannel(MaudeChannel channel);
    
    IReadOnlyList<MaudeMetric> GetMetricsForChannel(byte channelId);
    
    IReadOnlyList<MaudeMetric> GetMetricsForChannelInRange(MaudeChannel channel, DateTime fromUtc, DateTime toUtc);
    
    IReadOnlyList<MaudeMetric> GetMetricsForChannelInRange(byte channelId, DateTime fromUtc, DateTime toUtc);
    
    MaudeChannelSpan GetMetricsChannelSpanForRange(byte channelId, DateTime fromUtc, DateTime toUtc);
    
    void UseMetricsInChannelForRange(byte channelId, DateTime fromUtc, DateTime toUtc, Action<ReadOnlySpan<MaudeMetric>> useAction);
    
    IReadOnlyList<MaudeEvent> GetEventsForChannel(MaudeChannel channel);
    
    IReadOnlyList<MaudeEvent> GetEventsForChannel(byte channelId);
    
    IReadOnlyList<MaudeEvent> GetEventsForChannelInRange(MaudeChannel channel, DateTime fromUtc, DateTime toUtc);
    
    IReadOnlyList<MaudeEvent> GetEventsForChannelInRange(byte channelId, DateTime fromUtc, DateTime toUtc);
    
    void UseEventsInChannelForRange(byte channelId, DateTime fromUtc, DateTime toUtc, Action<ReadOnlySpan<MaudeEvent>> useAction);
}