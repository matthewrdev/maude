#if MACCATALYST
using System;
using UIKit;

namespace Maude;

/// <summary>
/// Registers macCatalyst-specific dependencies (presentation + FPS + window provider).
/// </summary>
public static class MaudeMacCatalystPlatform
{
    public static void Configure(Func<UIWindow?> windowProvider)
    {
        if (windowProvider == null) throw new ArgumentNullException(nameof(windowProvider));

        MaudeRuntimePlatform.Configure(windowProvider);
        MaudeRuntimePlatform.RegisterPresentationFactory((options, sink) => new MacCatalystNativePresentationService(options, sink, windowProvider));
        MaudeRuntimePlatform.RegisterFrameRateMonitorFactory(() => new MacCatalystFrameRateMonitor());
        MaudeLogger.Warning("MaudeMacCatalystPlatform.Configure is deprecated; platform setup now happens inside MaudeRuntime.Initialize. This call is safe to keep but no longer required.");
    }
}
#endif
