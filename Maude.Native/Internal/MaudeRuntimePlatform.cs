using System;
#if ANDROID
using Android.App;
#elif IOS || MACCATALYST
using UIKit;
#endif

namespace Maude;

/// <summary>
/// Allows platform heads to register factories for presentation and other platform services
/// without taking dependencies in the core assembly.
/// </summary>
public static class MaudeRuntimePlatform
{
    private static Func<MaudeOptions, IMaudeDataSink, IMaudePresentationService?>? presentationFactory;
    private static Func<IFrameRateMonitor>? frameRateMonitorFactory;

#if ANDROID
    public static Func<Android.App.Activity?>? CurrentActivityProvider { get; private set; }

    public static void Configure(Func<Android.App.Activity?>? activityProvider)
    {
        CurrentActivityProvider = activityProvider;
    }
#elif IOS
    public static Func<UIKit.UIWindow?>? CurrentWindowProvider { get; private set; }

    public static void Configure(Func<UIKit.UIWindow?>? windowProvider)
    {
        CurrentWindowProvider = windowProvider;
    }
#elif MACCATALYST
    public static Func<UIKit.UIWindow?>? CurrentWindowProvider { get; private set; }

    public static void Configure(Func<UIKit.UIWindow?>? windowProvider)
    {
        CurrentWindowProvider = windowProvider;
    }
#endif

    public static void RegisterPresentationFactory(Func<MaudeOptions, IMaudeDataSink, IMaudePresentationService?> factory)
    {
        presentationFactory = factory;
    }

    public static IMaudePresentationService? CreatePresentationService(MaudeOptions options, IMaudeDataSink dataSink)
    {
        return presentationFactory?.Invoke(options, dataSink);
    }

    public static void RegisterFrameRateMonitorFactory(Func<IFrameRateMonitor> factory)
    {
        frameRateMonitorFactory = factory;
    }

    public static IFrameRateMonitor CreateFrameRateMonitorFallback(Func<IFrameRateMonitor> defaultFactory)
    {
        return frameRateMonitorFactory?.Invoke() ?? defaultFactory();
    }
}
