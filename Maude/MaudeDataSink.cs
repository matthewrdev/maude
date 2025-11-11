namespace Maude;

internal class MaudeMutableDataSink : IMaudeDataSink
{
    
    private readonly Lock channelLock = new Lock();
    private readonly Lock metricsLock = new Lock();
    private readonly Lock eventsLock = new Lock();


    public event EventHandler<MaudeEventsUpdatedEventArgs>? OnChannelsUpdated;
    public event EventHandler<MaudeEventsUpdatedEventArgs>? OnMetricsUpdated;
    public event EventHandler<MaudeEventsUpdatedEventArgs>? OnEventsUpdated;
    
    public IReadOnlyList<MaudeChannel> Channels { get; }
    
    public IReadOnlyList<MaudeMetric> Metrics { get; }
    
    public IReadOnlyList<MaudeEvent> Events { get; }
    
    public IReadOnlyList<MaudeMetric> GetMetricsForChannel(MaudeChannel channel)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<MaudeMetric> GetMetricsForChannel(byte channelId)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<MaudeMetric> GetEventsForChannel(MaudeChannel channel)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<MaudeMetric> GetEventsForChannel(byte channelId)
    {
        throw new NotImplementedException();
    }
}