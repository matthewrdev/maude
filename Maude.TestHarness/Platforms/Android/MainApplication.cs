using Android.App;
using Android.Runtime;

namespace Maude.TestHarness;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        MaudeRuntime.Initialize();
        MaudeRuntime.Activate();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}