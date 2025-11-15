using Android.App;
using Android.Runtime;

namespace Maude.TestHarness;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        MaudeHarnessConfiguration.EnsureInitialized();
        MaudeRuntime.Activate();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
