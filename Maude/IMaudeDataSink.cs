namespace Maude;

/// <summary>
/// The 
/// </summary>
public interface IMaudeDataSink
{
    // Events for addition of channels, metrics, events.
    
    IReadOnlyList<MaudeChannel> Channels { get; }
    
    IReadOnlyList<MaudeMetric> Metrics { get; }
    
    IReadOnlyList<MaudeEvent> Events { get; }
    
    IReadOnlyList<MaudeMetric> GetMetricsForChannel(MaudeChannel channel);
    IReadOnlyList<MaudeMetric> GetMetricsForChannel(int channelId);
    
    
    IReadOnlyList<MaudeMetric> GetEventsForChannel(MaudeChannel channel);
    IReadOnlyList<MaudeMetric> GetEventsForChannel(int channelId);
}