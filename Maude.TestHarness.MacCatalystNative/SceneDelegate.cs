using Foundation;
using UIKit;

namespace Maude.TestHarness.MacCatalystNative;

[Register("SceneDelegate")]
public class SceneDelegate : UIResponder, IUIWindowSceneDelegate
{
    [Export("window")]
    public UIWindow? Window { get; set; }

    [Export("scene:willConnectToSession:options:")]
    public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene)
        {
            return;
        }

        var window = new UIWindow(windowScene);
        var root = new HarnessViewController();
        window.RootViewController = root;
        window.MakeKeyAndVisible();

        Window = window;

        MaudeRuntime.InitializeAndActivate();
    }
}
