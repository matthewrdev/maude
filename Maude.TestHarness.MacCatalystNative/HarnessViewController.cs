using UIKit;
using Maude;

namespace Maude.TestHarness.MacCatalystNative;

internal sealed class HarnessViewController : UIViewController
{
    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        View.BackgroundColor = UIColor.SystemBackground;

        var buttons = new[]
        {
            BuildButton("Present Sheet", () => MaudeRuntime.PresentSheet()),
            BuildButton("Present Overlay", () => MaudeRuntime.PresentOverlay()),
            BuildButton("Dismiss Overlay", () => MaudeRuntime.DismissOverlay()),
            BuildButton("Overlay Top-Left", () => MaudeRuntime.PresentOverlay(MaudeOverlayPosition.TopLeft)),
            BuildButton("Overlay Top-Right", () => MaudeRuntime.PresentOverlay(MaudeOverlayPosition.TopRight)),
            BuildButton("Overlay Bottom-Left", () => MaudeRuntime.PresentOverlay(MaudeOverlayPosition.BottomLeft)),
            BuildButton("Overlay Bottom-Right", () => MaudeRuntime.PresentOverlay(MaudeOverlayPosition.BottomRight)),
            BuildButton("Theme: Light", () => MaudeRuntime.ChartTheme = MaudeChartTheme.Light),
            BuildButton("Theme: Dark", () => MaudeRuntime.ChartTheme = MaudeChartTheme.Dark),
        };

        var stack = new UIStackView(buttons)
        {
            Axis = UILayoutConstraintAxis.Vertical,
            Distribution = UIStackViewDistribution.FillEqually,
            Alignment = UIStackViewAlignment.Fill,
            Spacing = 8,
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
