using System;

namespace Maude;

/// <summary>
/// Lightweight platform bridge supplying native handles without taking a dependency on MAUI.
/// The host app (or the MAUI head) should set the relevant providers during startup.
/// </summary>
public static class PlatformContext
{
#if ANDROID
    public static Func<Android.App.Activity?>? CurrentActivityProvider { get; private set; }

    public static void Configure(Func<Android.App.Activity?>? activityProvider)
    {
        CurrentActivityProvider = activityProvider;
    }
#endif

#if IOS
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
}
