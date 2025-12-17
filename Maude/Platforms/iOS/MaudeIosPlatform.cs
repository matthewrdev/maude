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

        MaudeRuntimePlatform.Configure(windowProvider);
        MaudeRuntimePlatform.RegisterPresentationFactory((options, sink) => new IosNativePresentationService(options, sink, windowProvider));
        MaudeRuntimePlatform.RegisterFrameRateMonitorFactory(() => new IosFrameRateMonitor());
        MaudeLogger.Warning("MaudeIosPlatform.Configure is deprecated; platform setup now happens inside MaudeRuntime.Initialize. This call is safe to keep but no longer required.");
    }
}
#endif
