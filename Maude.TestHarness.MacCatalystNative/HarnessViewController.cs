using UIKit;

namespace Maude.TestHarness.MacCatalystNative;

internal sealed class HarnessViewController : UIViewController
{
    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        View.BackgroundColor = UIColor.SystemBackground;

        var presentSheet = BuildButton("Present Sheet", () => MaudeRuntime.PresentSheet());
        var presentOverlay = BuildButton("Present Overlay", () => MaudeRuntime.PresentOverlay());
        var dismissOverlay = BuildButton("Dismiss Overlay", () => MaudeRuntime.DismissOverlay());

        var stack = new UIStackView(new[] { presentSheet, presentOverlay, dismissOverlay })
        {
            Axis = UILayoutConstraintAxis.Vertical,
            Distribution = UIStackViewDistribution.FillEqually,
            Alignment = UIStackViewAlignment.Center,
            Frame = View.Bounds
        };

        stack.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        View.AddSubview(stack);
    }

    private UIButton BuildButton(string text, Action action)
    {
        var button = UIButton.FromType(UIButtonType.System);
        button.SetTitle(text, UIControlState.Normal);
        button.TouchUpInside += (_, _) => action();
        return button;
    }
}
