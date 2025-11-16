namespace Maude;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Configures how Maude samples, retains, logs, and presents runtime data.
/// </summary>
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
    
    public List<MaudeChannel> AdditionalChannels { get; set; } = new();

    public bool AllowShakeGesture { get; private set; } = false;
    
    public MaudeShakeGestureBehaviour ShakeGestureBehaviour { get; private set; } = MaudeShakeGestureBehaviour.SlideSheet;
    
    public IMaudeLogCallback? AdditionalLogger { get; private set; }

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
                throw new InvalidOperationException("One or more additional channels use a reserved channel ID. " + string.Join(", ", usesPredefinedChannels.Select(x => x.Name + " uses reserved channel " + x.Id)));
            }
        }
    }
    
    public static MaudeOptionsBuilder CreateBuilder() => new MaudeOptionsBuilder();

    
    /// <summary>
    /// Fluent builder for <see cref="MaudeOptions"/>.
    /// </summary>
    public sealed class MaudeOptionsBuilder
    {
        private readonly MaudeOptions options = new MaudeOptions();

        public MaudeOptionsBuilder WithSampleFrequencyMilliseconds(ushort sampleFrequencyMilliseconds)
        {
            options.SampleFrequencyMilliseconds = sampleFrequencyMilliseconds;
            return this;
        }
        
        public MaudeOptionsBuilder WithRetentionPeriodSeconds(ushort retentionPeriodSeconds)
        {
            options.RetentionPeriodSeconds = retentionPeriodSeconds;
            return this;
        }

        public MaudeOptionsBuilder WithAdditionalChannels(IEnumerable<MaudeChannel> additionalChannels)
        {
            if (additionalChannels == null) throw new ArgumentNullException(nameof(additionalChannels));
            
            options.AdditionalChannels = additionalChannels.ToList();
            return this;
        }

        public MaudeOptionsBuilder AddAdditionalChannel(MaudeChannel additionalChannel)
        {
            if (additionalChannel == null) throw new ArgumentNullException(nameof(additionalChannel));

            options.AdditionalChannels ??= new List<MaudeChannel>();
            options.AdditionalChannels.Add(additionalChannel);
            return this;
        }

        public MaudeOptionsBuilder WithShakeGestureBehaviour(MaudeShakeGestureBehaviour  shakeGestureBehaviour)
        {
            options.ShakeGestureBehaviour = shakeGestureBehaviour;
            return this;
        }
        
        public MaudeOptionsBuilder WithShakeGesture()
        {
            options.AllowShakeGesture = true;
            return this;
        }

        public MaudeOptionsBuilder WithAdditionalLogger(IMaudeLogCallback logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            
            options.AdditionalLogger = logger;
            return this;
        }
        
        public MaudeOptionsBuilder WithBuiltInLogger()
        {
            options.AdditionalLogger = new MaudeConsoleLogger();
            return this;
        }

        public MaudeOptions Build()
        {
            options.Validate();
            return options;
        }
    }

}
