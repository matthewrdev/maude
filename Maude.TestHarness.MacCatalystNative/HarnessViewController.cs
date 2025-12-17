using UIKit;
using Maude;

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

        button.BackgroundColor = ToUiColor(MaudeConstants.MaudeBrandColor);
        button.SetTitleColor(UIColor.White, UIControlState.Normal);
        button.Layer.CornerRadius = 10;
        button.ContentEdgeInsets = new UIEdgeInsets(10, 16, 10, 16);

        return button;
    }

    private static UIColor ToUiColor(Color color)
    {
        return UIColor.FromRGBA(color.RedNormalized, color.GreenNormalized, color.BlueNormalized, color.AlphaNormalized);
    }
}
