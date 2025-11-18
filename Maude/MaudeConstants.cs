

namespace Maude;

/// <summary>
/// Common constants used across the Maude runtime and UI.
/// </summary>
public static class MaudeConstants
{
    internal const string LoggingPrefix = " 🟣MAUDE: ";

    public const MaudeEventType DefaultEventType = MaudeEventType.Info;
    public const string DefaultEventSymbol = "i";

    public const ushort DefaultSampleFrequencyMilliseconds = 500;
    
    public const ushort MinSampleFrequencyMilliseconds = 200;
    
    public const ushort MaxSampleFrequencyMilliseconds = 2000;
    
    public const ushort DefaultRetentionPeriodSeconds = 10 * 60;
    
    public const ushort MinRetentionPeriodSeconds = 1 * 60;
    
    public const ushort MaxRetentionPeriodSeconds = 60 * 60;
    
    public static readonly Color MaudeBrandColor = new Color(91, 58, 150);
    
    public static readonly Color MaudeBrandColor_Faded = new Color(91, 58, 150).WithAlpha(0.8f);

    /// <summary>
    /// Reserved channel identifiers and display metadata.
    /// </summary>
    public static class ReservedChannels
    {
        public const byte ClrMemoryUsage_Id = 0;
        public const byte FramesPerSecond_Id = 3;

        public const byte ChannelNotSpecified_Id = byte.MaxValue;
        
        public static readonly Color ClrMemoryUsage_Color = new Color(92, 45, 144);
        public const string ClrMemoryUsage_Name = ".NET";
        
        public const string FramesPerSecond_Name = "FPS";
        public static readonly Color FramesPerSecond_Color = new Color(35, 181, 115);
    
#if IOS
        public const byte PlatformMemoryUsage_Id = 1;
        public const string PlatformMemoryUsage_Name = "iOS";
        public static readonly Color PlatformMemoryUsage_Color = new Color(0, 122, 255);
#elif ANDROID
        public const byte NativeHeapAllocated_Id = 1;
        public const byte Rss_Id = 2;

        public const string NativeHeapAllocated_Name = "Native heap";
        public const string Rss_Name = "RSS";

        public static readonly Color NativeHeapAllocated_Color= new Color(0, 122, 255);
        public static readonly Color Rss_Color = new Color(200, 140, 30);
#else

        public static readonly Color PlatformMemoryUsage_Color = new Color(92, 45, 144);
        public const string PlatformMemoryUsage_Name = "Not Applicable";
#endif
        
    }

    public static bool IsPredefinedChannel(MaudeChannel channel)
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));
        
        return channel.Id ==  MaudeConstants.ReservedChannels.ClrMemoryUsage_Id
            || channel.Id == MaudeConstants.ReservedChannels.FramesPerSecond_Id
#if IOS
            || channel.Id == MaudeConstants.ReservedChannels.PlatformMemoryUsage_Id
#elif ANDROID
            || channel.Id == MaudeConstants.ReservedChannels.NativeHeapAllocated_Id
            || channel.Id == MaudeConstants.ReservedChannels.Rss_Id
#endif
            || channel.Id ==  MaudeConstants.ReservedChannels.ChannelNotSpecified_Id;
    }
}
