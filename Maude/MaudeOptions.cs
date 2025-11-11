namespace Maude;

public struct MaudeOptions
{
    public static readonly MaudeOptions Default = new MaudeOptions()
    {
        SampleFrequencyMilliseconds = MaudeConstants.DefaultSampleFrequencyMilliseconds,
        RetentionPeriodSeconds = MaudeConstants.DefaultRetentionPeriodSeconds,
    };
    
    public required ushort SampleFrequencyMilliseconds { get; init; }
        
    public required ushort RetentionPeriodSeconds { get; init; }
}