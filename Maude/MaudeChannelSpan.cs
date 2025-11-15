namespace Maude;

/// <summary>
/// Defines the upper and lower values of a captured metrics channel for a time period.
/// </summary>
public struct MaudeChannelSpan
{
    /// <summary>
    /// The channel ID that this span applies to.
    /// </summary>
    public required byte ChannelId { get; init; }
    
    /// <summary>
    /// The minimum value of the metrics for the <see cref="ChannelId"/> within this span.
    /// </summary>
    public required long MinValue { get; init; }
    
    /// <summary>
    /// The maximum value of the metrics for the <see cref="ChannelId"/> within this span.
    /// </summary>
    public required long MaxValue { get; init; }
    
    /// <summary>
    /// The date time, in UTC, that this channel span starts at.
    /// </summary>
    public required DateTime FromUtc { get; init; }
    
    /// <summary>
    /// The date time, in UTC, that this channel span ends at.
    /// </summary>
    public required DateTime ToUtc { get; init; }
    
    /// <summary>
    /// If the computed channel span has metrics within the provided <see cref="FromUtc"/> and <see cref="ToUtc"/>. 
    /// </summary>
    public required bool Valid  { get; init; }
    
    /// <summary>
    /// The total number of recorded metrics in this span. 
    /// </summary>
    public required int Count  { get; init; }

    public static readonly MaudeChannelSpan Invalid = new MaudeChannelSpan()
    {
        ChannelId = 0,
        MinValue = 0,
        MaxValue = long.MaxValue,
        FromUtc = DateTime.MinValue,
        ToUtc = DateTime.MaxValue,
        Count = 0,
        Valid = false,
    };
}