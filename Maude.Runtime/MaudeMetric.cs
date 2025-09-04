namespace Maude.Runtime;

public struct MaudeMetric
{
    public ulong Value { get; }
    
    public DateTime CapturedAtUtc { get; }
    
    public int Channel { get; }
    
}