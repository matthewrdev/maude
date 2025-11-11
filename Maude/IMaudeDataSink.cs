namespace Maude;

/// <summary>
/// The 
/// </summary>
public interface IMaudeDataSink
{
    event EventHandler OnUpdated;
    
    IReadOnlyList<MaudeChannel> GetChannels();
    
    IReadOnlyList<MaudeMetric> GetMetricsForChannel(MaudeChannel channel);
    
    IReadOnlyList<MaudeMetric> GetMetricsForChannel(byte channelId);
    
    IReadOnlyList<MaudeMetric> GetMetricsForChannelInRange(MaudeChannel channel, DateTime fromUtc, DateTime toUtc);
    
    IReadOnlyList<MaudeMetric> GetMetricsForChannelInRange(byte channelId, DateTime fromUtc, DateTime toUtc);
    
    
    IReadOnlyList<MaudeEvent> GetEventsForChannel(MaudeChannel channel);
    
    IReadOnlyList<MaudeEvent> GetEventsForChannel(byte channelId);
    
    IReadOnlyList<MaudeEvent> GetEventsForChannelInRange(MaudeChannel channel);
    
    IReadOnlyList<MaudeEvent> GetEventsForChannelInRange(byte channelId, DateTime fromUtc, DateTime toUtc);
    
    IReadOnlyList<MaudeEvent> GetChannellessEventsInRange(DateTime fromUtc, DateTime toUtc);
}