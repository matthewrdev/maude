namespace Maude;

public class MaudeOptions
{
    public static readonly MaudeOptions Default = new MaudeOptions()
    {
        SampleFrequencyMilliseconds = MaudeConstants.DefaultSampleFrequencyMilliseconds,
        RetentionPeriodSeconds = MaudeConstants.DefaultRetentionPeriodSeconds,
        AdditionalChannels = new List<MaudeChannel>()
    };
    
    public ushort SampleFrequencyMilliseconds { get; private set; } =  MaudeConstants.DefaultSampleFrequencyMilliseconds;
        
    public ushort RetentionPeriodSeconds { get; private set; } =  MaudeConstants.DefaultRetentionPeriodSeconds;
    
    public int MaximumBufferSize => RetentionPeriodSeconds * (int)Math.Ceiling(1000f / (float)SampleFrequencyMilliseconds);
    
    public required List<MaudeChannel> AdditionalChannels { get; init; }

    public void Validate()
    {
        if (SampleFrequencyMilliseconds > MaudeConstants.MaxSampleFrequencyMilliseconds)
        {
            MaudeLogger.Warning($"The 'SampleFrequencyMilliseconds' was above the minimum frequency of '{MaudeConstants.MaxSampleFrequencyMilliseconds}' milliseconds. The sampling rate has been coerced to '{MaudeConstants.MaxSampleFrequencyMilliseconds}'");
            SampleFrequencyMilliseconds =  MaudeConstants.MaxSampleFrequencyMilliseconds;
        }

        if (SampleFrequencyMilliseconds < MaudeConstants.MinSampleFrequencyMilliseconds)
        {
            MaudeLogger.Warning($"The 'SampleFrequencyMilliseconds' was below the minimum frequency of '{MaudeConstants.MinSampleFrequencyMilliseconds}' milliseconds. The sampling rate has been coerced to '{MaudeConstants.MinSampleFrequencyMilliseconds}'");
            SampleFrequencyMilliseconds =  MaudeConstants.MinSampleFrequencyMilliseconds;
        }
        
        if (RetentionPeriodSeconds > MaudeConstants.MaxRetentionPeriodSeconds)
        {
            MaudeLogger.Warning($"The 'RetentionPeriodSeconds' was above the maximum retention of '{MaudeConstants.MaxRetentionPeriodSeconds}' seconds. The retention range has been coerced to '{MaudeConstants.MaxRetentionPeriodSeconds}'");
            RetentionPeriodSeconds =  MaudeConstants.MaxRetentionPeriodSeconds;
        }
        
        if (RetentionPeriodSeconds < MaudeConstants.MinRetentionPeriodSeconds)
        {
            MaudeLogger.Warning($"The 'RetentionPeriodSeconds' was below the minimum retention of '{MaudeConstants.MinRetentionPeriodSeconds}' seconds. The retention range has been coerced to '{MaudeConstants.MinRetentionPeriodSeconds}'");
            RetentionPeriodSeconds =  MaudeConstants.MinRetentionPeriodSeconds;
        }
        
        if (AdditionalChannels != null && AdditionalChannels.Count > 0)
        {
            var usesPredefinedChannels = AdditionalChannels.Where(MaudeConstants.IsPredefinedChannel).ToList();
            if (usesPredefinedChannels.Any())
            {
                throw new InvalidOperationException("One or more additional channels use a reserved Maude channel ID. " + string.Join(", ", usesPredefinedChannels.Select(x => x.Name + " uses reserved channel " + x.Id)));
            }
        }
    }
}