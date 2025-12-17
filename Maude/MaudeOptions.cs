namespace Maude;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Configures how Maude samples, retains, logs, and presents runtime data.
/// </summary>
    public class MaudeOptions
    {
        /// <summary>
        /// Default options instance: 500ms sampling, 10-minute retention, no shake gesture, overlay top right, FPS off.
        /// </summary>
        public static readonly MaudeOptions Default = new MaudeOptions()
        {
            SampleFrequencyMilliseconds = MaudeConstants.DefaultSampleFrequencyMilliseconds,
            RetentionPeriodSeconds = MaudeConstants.DefaultRetentionPeriodSeconds,
            AdditionalChannels = new List<MaudeChannel>(),
            DefaultMemoryChannels = MaudeDefaultMemoryChannels.PlatformDefaults,
            AllowShakeGesture = false,
            ShakeGestureBehaviour = MaudeShakeGestureBehaviour.SlideSheet,
            AdditionalLogger = new MaudeConsoleLogger(),
            DefaultOverlayPosition= MaudeOverlayPosition.TopRight,
            EnableFramesPerSecond = true,
            EventRenderingBehaviour = MaudeEventRenderingBehaviour.IconsOnly
        };
        
        /// <summary>
        /// Sampling cadence in milliseconds.
        /// </summary>
        public ushort SampleFrequencyMilliseconds { get; private set; } =  MaudeConstants.DefaultSampleFrequencyMilliseconds;
        
        /// <summary>
        /// How long metric/event samples are retained before trim, in seconds.
        /// </summary>
        public ushort RetentionPeriodSeconds { get; private set; } =  MaudeConstants.DefaultRetentionPeriodSeconds;
        
        /// <summary>
        /// Maximum buffered samples calculated from retention and frequency.
        /// </summary>
        public int MaximumBufferSize => RetentionPeriodSeconds * (int)Math.Ceiling(1000f / (float)SampleFrequencyMilliseconds);
        
        /// <summary>
        /// Additional metric/event channels to plot besides the built-in ones.
        /// </summary>
        public List<MaudeChannel> AdditionalChannels { get; private set; } = new();
        
        /// <summary>
        /// Controls which of the built-in memory channels should be exposed.
        /// </summary>
        public MaudeDefaultMemoryChannels DefaultMemoryChannels { get; private set; } = MaudeDefaultMemoryChannels.PlatformDefaults;

        /// <summary>
        /// Allow shake gesture to present the UI.
        /// </summary>
        public bool AllowShakeGesture { get; private set; } = false;
        
        /// <summary>
        /// Optional predicate that determines whether the shake gesture should be considered active.
        /// </summary>
        public Func<bool>? ShakeGesturePredicate { get; private set; }
        
        /// <summary>
        /// Behaviour applied when a shake is detected.
        /// </summary>
        public MaudeShakeGestureBehaviour ShakeGestureBehaviour { get; private set; } = MaudeShakeGestureBehaviour.SlideSheet;

        /// <summary>
        /// Enable capturing and rendering frames-per-second metrics at startup.
        /// </summary>
        public bool EnableFramesPerSecond { get; private set; } = false;
        
        /// <summary>
        /// Default overlay anchor position when presented without an explicit position.
        /// </summary>
        public MaudeOverlayPosition DefaultOverlayPosition { get; private set; } = MaudeOverlayPosition.TopRight;
        
        /// <summary>
        /// Optional additional logger to receive Maude log messages.
        /// </summary>
        public IMaudeLogCallback? AdditionalLogger { get; private set; }
        
        /// <summary>
        /// Configures how annotated events should appear on the chart.
        /// </summary>
        public MaudeEventRenderingBehaviour EventRenderingBehaviour { get; private set; } = MaudeEventRenderingBehaviour.IconsOnly;

        /// <summary>
        /// Optional save snapshot action rendered in the slide sheet.
        /// </summary>
        public MaudeSaveSnapshotAction? SaveSnapshotAction { get; internal set; }

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

            /// <summary>
            /// Sets the sampling cadence in milliseconds.
            /// </summary>
            public MaudeOptionsBuilder WithSampleFrequencyMilliseconds(ushort sampleFrequencyMilliseconds)
            {
                options.SampleFrequencyMilliseconds = sampleFrequencyMilliseconds;
                return this;
            }

            /// <summary>
            /// Enables frames-per-second sampling and charting at startup.
            /// </summary>
            public MaudeOptionsBuilder WithFramesPerSecond()
            {
                options.EnableFramesPerSecond = true;
                return this;
            }
            
            /// <summary>
            /// Sets the retention period, in seconds, for buffered samples.
            /// </summary>
            public MaudeOptionsBuilder WithRetentionPeriodSeconds(ushort retentionPeriodSeconds)
            {
                options.RetentionPeriodSeconds = retentionPeriodSeconds;
                return this;
            }

            /// <summary>
            /// Replaces the additional channels collection.
            /// </summary>
            public MaudeOptionsBuilder WithAdditionalChannels(IEnumerable<MaudeChannel> additionalChannels)
            {
                if (additionalChannels == null) throw new ArgumentNullException(nameof(additionalChannels));
                
                options.AdditionalChannels = additionalChannels.ToList();
                return this;
            }

            /// <summary>
            /// Adds a single additional channel to the collection.
            /// </summary>
            public MaudeOptionsBuilder AddAdditionalChannel(MaudeChannel additionalChannel)
            {
                if (additionalChannel == null) throw new ArgumentNullException(nameof(additionalChannel));

                options.AdditionalChannels ??= new List<MaudeChannel>();
                options.AdditionalChannels.Add(additionalChannel);
                return this;
            }

            /// <summary>
            /// Specifies which of the built-in memory channels should be displayed.
            /// </summary>
            public MaudeOptionsBuilder WithDefaultMemoryChannels(MaudeDefaultMemoryChannels memoryChannels)
            {
                options.DefaultMemoryChannels = memoryChannels;
                return this;
            }

            /// <summary>
            /// Removes the provided built-in memory channels from the configuration.
            /// </summary>
            public MaudeOptionsBuilder WithoutDefaultMemoryChannels(MaudeDefaultMemoryChannels memoryChannels)
            {
                options.DefaultMemoryChannels &= ~memoryChannels;
                return this;
            }

            /// <summary>
            /// Configures the shake gesture behaviour.
            /// </summary>
            public MaudeOptionsBuilder WithShakeGestureBehaviour(MaudeShakeGestureBehaviour  shakeGestureBehaviour)
            {
                options.ShakeGestureBehaviour = shakeGestureBehaviour;
                return this;
            }
            
            /// <summary>
            /// Enables handling of device shake gestures.
            /// </summary>
            public MaudeOptionsBuilder WithShakeGesture()
            {
                options.AllowShakeGesture = true;
                return this;
            }

            /// <summary>
            /// Configures a predicate evaluated before enabling or responding to shake gestures.
            /// </summary>
            public MaudeOptionsBuilder WithShakeGesturePredicate(Func<bool> predicate)
            {
                if (predicate == null) throw new ArgumentNullException(nameof(predicate));
                
                options.ShakeGesturePredicate = predicate;
                return this;
            }

            /// <summary>
            /// Sets the default overlay anchor position used when none is specified.
            /// </summary>
            public MaudeOptionsBuilder WithDefaultOverlayPosition(MaudeOverlayPosition position)
            {
                options.DefaultOverlayPosition = position;
                return this;
            }

            /// <summary>
            /// Adds an external logger for Maude logs.
            /// </summary>
            public MaudeOptionsBuilder WithAdditionalLogger(IMaudeLogCallback logger)
            {
                if (logger == null) throw new ArgumentNullException(nameof(logger));
                
                options.AdditionalLogger = logger;
                return this;
            }
            
            /// <summary>
            /// Configures how events are rendered on the chart.
            /// </summary>
            public MaudeOptionsBuilder WithEventRenderingBehaviour(MaudeEventRenderingBehaviour behaviour)
            {
                options.EventRenderingBehaviour = behaviour;
                return this;
            }

            /// <summary>
            /// Enables a custom save snapshot action in the slide sheet.
            /// </summary>
            /// <param name="copyDelegate">Delegate invoked with the captured <see cref="MaudeSnapshot"/>.</param>
            /// <param name="label">Text displayed on the action button.</param>
            public MaudeOptionsBuilder WithSaveSnapshotAction(Func<MaudeSnapshot, Task> copyDelegate, string label)
            {
                if (copyDelegate == null) throw new ArgumentNullException(nameof(copyDelegate));
                if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));

                options.SaveSnapshotAction = new MaudeSaveSnapshotAction(label, copyDelegate);
                return this;
            }
            
            /// <summary>
            /// Enables the built-in console logger.
            /// </summary>
            public MaudeOptionsBuilder WithBuiltInLogger()
            {
                options.AdditionalLogger = new MaudeConsoleLogger();
                return this;
            }

            /// <summary>
            /// Validates and returns the configured options.
            /// </summary>
            public MaudeOptions Build()
            {
                options.Validate();
                return options;
            }
    }

}
