namespace Maude;

public class MaudeSnapshot
{
    public List<MaudeChannel>? Channels { get; set; }
    
    public List<MaudeMetricsSnapshot>? Metrics { get; set; }
    
    public List<MaudeEventsSnapshot>? Events { get; set; }

}

public class MaudeMetricsSnapshot
{
    public byte ChannelId { get; set; }
    
    public List<MaudeMetric>? Metrics { get; set; }
}


public class MaudeEventsSnapshot
{
    public byte ChannelId { get; set; }
    
    public List<MaudeEvent>? Events { get; set; }

}
