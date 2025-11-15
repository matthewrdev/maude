namespace Maude;

public struct MaudeRenderOptions
{
    public required IReadOnlyList<byte> Channels { get; init; }
    
    public required DateTime FromUtc { get; init; }
    
    public required DateTime ToUtc { get; init; }
    
    public required DateTime? CurrentUtc { get; init; }
    
    public required MaudeChartRenderMode Mode { get; init; }

    public required float? ProbePosition { get; init; }
}
