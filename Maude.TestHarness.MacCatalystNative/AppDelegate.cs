using Foundation;
using UIKit;

namespace Maude.TestHarness.MacCatalystNative;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        return true;
    }
}
