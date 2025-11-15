using Android.App;
using Android.Runtime;

namespace Maude.TestHarness;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        MaudeLogger.RegisterCallback(new CustomMaudeLogCallback());
        MaudeRuntime.Initialize(CustomMaudeConfiguration.Options);
        MaudeRuntime.Activate();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
