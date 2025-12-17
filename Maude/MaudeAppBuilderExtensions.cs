using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Hosting;

namespace Maude;

/// <summary>
/// Extension methods for wiring Maude into a MAUI application builder.
/// </summary>
public static class MaudeAppBuilderExtensions
{
    /// <summary>
    /// Set's up the <see cref="MauiAppBuilder"/> to initialise the <see cref="MaudeRuntime"/> and registers required fonts and dependencies. 
    /// <para/>
    /// Does not activate the memory tracking, please use <see cref="MaudeRuntime.Activate"/> to start tracking memory usage.
    /// </summary>
    public static MauiAppBuilder UseMaude(this MauiAppBuilder builder, MaudeOptions? maudeOptions = null)
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
        if (!MaudeRuntime.IsInitialized)
        {
            MaudeRuntime.Initialize(maudeOptions);
        }

        builder.Services.AddSingleton<IMaudeRuntime>(_ => MaudeRuntime.Instance);
        builder.Services.AddSingleton<IMaudeDataSink>(_ => MaudeRuntime.Instance.DataSink);
        
        return builder;
    }
    /// <summary>
    /// Set's up the <see cref="MauiAppBuilder"/> to initialise the <see cref="MaudeRuntime"/> and registers required fonts and dependencies.
    /// <para/>
    /// Immediately activates Maudes memory tracking.
    /// </summary>
    public static MauiAppBuilder UseMaudeAndActivate(this MauiAppBuilder builder, MaudeOptions? maudeOptions = null)
    {
        builder = builder.UseMaude(maudeOptions);
        
        MaudeRuntime.Activate();
        
        return builder;
    }
}
