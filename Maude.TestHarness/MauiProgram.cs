using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Maude.TestHarness;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        PlatformMaudeConfigurator.Configure();

        var builder = MauiApp.CreateBuilder();
        var maudeOptions = MaudeOptions.CreateBuilder()
            .WithAdditionalLogger(new CustomMaudeLogCallback())
            .WithShakeGesture()
            .WithMauiWindowProvider()
            .WithSampleFrequencyMilliseconds(400)
            .WithRetentionPeriodSeconds(120)
            .WithShakeGestureBehaviour(MaudeShakeGestureBehaviour.Overlay)
            .WithShakeGesturePredicate(() => ShakePredicateCoordinator.ShouldAllowShake)
            .WithAdditionalChannels(CustomMaudeConfiguration.AdditionalChannels)
            .WithSaveSnapshotAction(SnapshotActionHelper.CopySnapshotToClipboardAsync, "COPY")
            .Build();

        builder
            .UseMauiApp<App>()
            .UseMaude(maudeOptions)
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Activate after platform services are registered by UseMaude.
        MaudeRuntime.Activate();

        return app;
    }
}
