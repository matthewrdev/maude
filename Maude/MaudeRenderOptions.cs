namespace Maude;

public struct MaudeRenderOptions
{
    public required IReadOnlyCollection<byte> Channels { get; init; }
    
    public required DateTime FromUtc { get; init; }
    
    public required DateTime ToUtc { get; init; }
    
    public required DateTime? CurrentUtc { get; init; }
}