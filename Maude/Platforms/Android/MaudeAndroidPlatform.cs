#if ANDROID
using System;
using Android.App;

namespace Maude;

/// <summary>
/// Registers Android-specific dependencies (presentation, FPS monitor, activity provider).
/// Host apps should call this early, e.g., in Application.OnCreate or via the MAUI head.
/// </summary>
public static class MaudeAndroidPlatform
{
    public static void Configure(Func<Activity?> currentActivityProvider)
    {
        if (currentActivityProvider == null) throw new ArgumentNullException(nameof(currentActivityProvider));

        PlatformContext.Configure(currentActivityProvider);
        MaudeRuntimePlatform.RegisterPresentationFactory((options, sink) => new AndroidNativePresentationService(options, sink));
    }
}
#endif
