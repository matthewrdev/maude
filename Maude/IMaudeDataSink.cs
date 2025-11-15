namespace Maude;

/// <summary>
/// The 
/// </summary>
public interface IMaudeDataSink
{
    public IReadOnlyCollection<MaudeChannel> Channels { get; }
    
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
    IReadOnlyCollection<MaudeMetric> Metrics { get; }
    
    /// <summary>
    /// 
    /// </summary>
    IReadOnlyCollection<MaudeEvent> Events { get; }
    
    IReadOnlyCollection<MaudeMetric> GetMetricsForChannel(MaudeChannel channel);
    
    IReadOnlyCollection<MaudeMetric> GetMetricsForChannel(byte channelId);
    
    IReadOnlyCollection<MaudeMetric> GetMetricsForChannelInRange(MaudeChannel channel, DateTime fromUtc, DateTime toUtc);
    
    IReadOnlyCollection<MaudeMetric> GetMetricsForChannelInRange(byte channelId, DateTime fromUtc, DateTime toUtc);
    
    IReadOnlyCollection<MaudeEvent> GetEventsForChannel(MaudeChannel channel);
    
    IReadOnlyCollection<MaudeEvent> GetEventsForChannel(byte channelId);
    
    IReadOnlyCollection<MaudeEvent> GetEventsForChannelInRange(MaudeChannel channel, DateTime fromUtc, DateTime toUtc);
    
    IReadOnlyCollection<MaudeEvent> GetEventsForChannelInRange(byte channelId, DateTime fromUtc, DateTime toUtc);
}