using UIKit;

namespace Maude.TestHarness.iOSNative;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override UIWindow? Window { get; set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        Window = new UIWindow(UIScreen.MainScreen.Bounds);

        var root = new HarnessViewController();
        Window.RootViewController = root;
        Window.MakeKeyAndVisible();

        MaudeIosPlatform.Configure(() => Window);
        MaudeRuntime.InitializeAndActivate();

        return true;
    }
}
