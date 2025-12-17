using UIKit;

namespace Maude.TestHarness;

public static class Program
{
    static void Main(string[] args)
    {
        var options = MaudeOptions.CreateBuilder()
            .WithAdditionalLogger(new CustomMaudeLogCallback())
            .WithShakeGesture()
            .WithSampleFrequencyMilliseconds(400)
            .WithRetentionPeriodSeconds(120)
            .WithShakeGestureBehaviour(MaudeShakeGestureBehaviour.Overlay)
            .WithShakeGesturePredicate(() => ShakePredicateCoordinator.ShouldAllowShake)
            .WithAdditionalChannels(CustomMaudeConfiguration.AdditionalChannels)
            .WithSaveSnapshotAction(SnapshotActionHelper.CopySnapshotToClipboardAsync, "COPY")
            .Build();

        MaudeRuntime.InitializeAndActivate(options);

        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
