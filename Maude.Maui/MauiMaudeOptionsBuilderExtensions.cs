using System;
using System.Linq;
using Microsoft.Maui.ApplicationModel;

namespace Maude;

/// <summary>
/// Convenience helpers for wiring native window handles from a MAUI app into Maude options.
/// </summary>
public static class MauiMaudeOptionsBuilderExtensions
{
    /// <summary>
    /// Uses the current MAUI window/activity handle as the presentation window provider.
    /// </summary>
    public static MaudeOptions.MaudeOptionsBuilder WithMauiWindowProvider(this MaudeOptions.MaudeOptionsBuilder builder)
    {
#if ANDROID
        builder.WithPresentationWindowProvider(() => Platform.CurrentActivity);
#elif IOS || MACCATALYST
        builder.WithPresentationWindowProvider(() =>
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
#else
        builder.WithPresentationWindowProvider(() => null);
#endif
        return builder;
    }
}
