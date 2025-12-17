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

        PlatformContext.Configure(windowProvider);
        MaudeRuntimePlatform.RegisterPresentationFactory((options, sink) => new MacCatalystNativePresentationService(options, sink));
        MaudeRuntimePlatform.RegisterFrameRateMonitorFactory(() => new MacCatalystFrameRateMonitor());
    }
}
#endif
