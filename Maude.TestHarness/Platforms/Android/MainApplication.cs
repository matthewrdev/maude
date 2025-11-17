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
            .WithShakeGesture()
            .WithShakeGestureBehaviour(MaudeShakeGestureBehaviour.Overlay)
            .WithAdditionalChannels(CustomMaudeConfiguration.AdditionalChannels)
            .Build();
        
        MaudeRuntime.InitializeAndActivate(options);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
