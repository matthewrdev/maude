using Microsoft.Maui.ApplicationModel;

namespace Maude.TestHarness;

/// <summary>
/// Ensures platform presentation factories are registered before the MAUI app builder runs.
/// </summary>
internal static class PlatformMaudeConfigurator
{
    public static void Configure()
    {
#if ANDROID
        MaudeAndroidPlatform.Configure(() => Platform.CurrentActivity);
#elif IOS
        MaudeIosPlatform.Configure(() =>
        {
            return UIKit.UIApplication.SharedApplication
                       ?.ConnectedScenes
                       ?.OfType<UIKit.UIWindowScene>()
                       ?.SelectMany(scene => scene.Windows)
                       ?.FirstOrDefault(w => w.IsKeyWindow)
                   ?? UIKit.UIApplication.SharedApplication
                       ?.ConnectedScenes
                       ?.OfType<UIKit.UIWindowScene>()
                       ?.SelectMany(scene => scene.Windows)
                       ?.FirstOrDefault();
        });
#elif MACCATALYST
        MaudeMacCatalystPlatform.Configure(() =>
        {
            return UIKit.UIApplication.SharedApplication
                       ?.ConnectedScenes
                       ?.OfType<UIKit.UIWindowScene>()
                       ?.SelectMany(scene => scene.Windows)
                       ?.FirstOrDefault(w => w.IsKeyWindow)
                   ?? UIKit.UIApplication.SharedApplication
                       ?.ConnectedScenes
                       ?.OfType<UIKit.UIWindowScene>()
                       ?.SelectMany(scene => scene.Windows)
                       ?.FirstOrDefault();
        });
#endif
    }
}
