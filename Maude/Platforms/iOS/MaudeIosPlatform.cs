#if IOS
using System;
using UIKit;

namespace Maude;

/// <summary>
/// Registers iOS-specific dependencies (presentation, FPS monitor, window provider).
/// </summary>
public static class MaudeIosPlatform
{
    public static void Configure(Func<UIWindow?> windowProvider)
    {
        if (windowProvider == null) throw new ArgumentNullException(nameof(windowProvider));

        PlatformContext.Configure(windowProvider);
        MaudeRuntimePlatform.RegisterPresentationFactory((options, sink) => new IosNativePresentationService(options, sink));
        MaudeRuntimePlatform.RegisterFrameRateMonitorFactory(() => new IosFrameRateMonitor());
    }
}
#endif
