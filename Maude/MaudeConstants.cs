

namespace Maude;

public static class MaudeConstants
{
    internal const string LoggingPrefix = " 🟣 Maude: ";

    public const ushort DefaultSampleFrequencyMilliseconds = 500;
    
    public const ushort DefaultRetentionPeriodSeconds = 10 * 60;
    
    internal const string MaterialSymbolsFontName = "Maude-MaterialSymbols";

    public const byte ReservedMaudeChannel_ClrMemoryUsage = 0;
    
    public const byte ReservedMaudeChannel_PlatformMemoryUsage = 1;
    
    public const string ReservedMaudeChannelName_ClrMemoryUsage = ".NET";
    
    #if IOS
    public const string ReservedMaudeChannelName_PlatformMemoryUsage = "iOS";
    #elif ANDROID
    public const string ReservedMaudeChannelName_PlatformMemoryUsage = "Java";
    #else
    public const string ReservedMaudeChannelName_PlatformMemoryUsage = "Not Applicable";
    #endif
}