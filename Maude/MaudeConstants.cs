

namespace Maude;

public static class MaudeConstants
{
    internal const string LoggingPrefix = " 🟣 Maude: ";

    public const string DefaultEventIcon = MaterialSymbols.Info;

    public const ushort DefaultSampleFrequencyMilliseconds = 500;
    
    public const ushort MinSampleFrequencyMilliseconds = 200;
    
    public const ushort MaxSampleFrequencyMilliseconds = 2000;
    
    public const ushort DefaultRetentionPeriodSeconds = 10 * 60;
    
    public const ushort MinRetentionPeriodSeconds = 1 * 60;
    
    public const ushort MaxRetentionPeriodSeconds = 60 * 60;
    
    internal const string MaterialSymbolsFontName = "Maude-MaterialSymbols";
    
    public static readonly Color MaudeBrandColor = new Color(91, 58, 150);

    public static class ReservedChannels
    {
        public const byte ClrMemoryUsage_Id = 0;
    
        public const byte PlatformMemoryUsage_Id = 1;
    
        public const byte ChannelNotSpecified_Id = byte.MaxValue;
        
        public static readonly Color ClrMemoryUsage_Color = new Color(92, 45, 144);
    
        public const string ClrMemoryUsage_Name = ".NET";
    
#if IOS
        public const string PlatformMemoryUsage_Name = "iOS";
        public static readonly Color PlatformMemoryUsage_Color = new Color(0, 122, 255);
#elif ANDROID
        public const string PlatformMemoryUsage_Name = "Java";
        public static readonly Color PlatformMemoryUsage_Color = new Color(61, 220, 132);
#else

        public static readonly Color PlatformMemoryUsage_Color = new Color(92, 45, 144);
        public const string PlatformMemoryUsage_Name = "Not Applicable";
#endif
        
    }

    public static bool IsPredefinedChannel(MaudeChannel channel)
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));
        
        return channel.Id ==  MaudeConstants.ReservedChannels.ClrMemoryUsage_Id
            || channel.Id ==  MaudeConstants.ReservedChannels.PlatformMemoryUsage_Id
            || channel.Id ==  MaudeConstants.ReservedChannels.ChannelNotSpecified_Id;
    }
}