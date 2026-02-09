using Android.App;
using Android.Runtime;

namespace Maude.TestHarness;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        var options = MaudeOptions.CreateBuilder()
            .WithAdditionalLogger(new CustomMaudeLogCallback())
            .WithMauiWindowProvider()
            .WithShakeGesture()
            .WithShakeGestureBehaviour(MaudeShakeGestureBehaviour.Overlay)
            .WithShakeGesturePredicate(() => ShakePredicateCoordinator.ShouldAllowShake)
            .WithAdditionalChannels(CustomMaudeConfiguration.AdditionalChannels)
            .WithSaveSnapshotAction(SnapshotActionHelper.CopySnapshotToClipboardAsync, "COPY")
            .Build();
        
        MaudeRuntime.InitializeAndActivate(options);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
