using UIKit;

namespace Maude.TestHarness.iOSNative;

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
            BuildButton("Annotations: Labels + Icons", () => MaudeRuntime.EventRenderingBehaviour = MaudeEventRenderingBehaviour.LabelsAndIcons),
            BuildButton("Annotations: Icons Only", () => MaudeRuntime.EventRenderingBehaviour = MaudeEventRenderingBehaviour.IconsOnly),
            BuildButton("Annotations: None", () => MaudeRuntime.EventRenderingBehaviour = MaudeEventRenderingBehaviour.None),
            BuildButton("Theme: Light", () => MaudeRuntime.ChartTheme = MaudeChartTheme.Light),
            BuildButton("Theme: Dark", () => MaudeRuntime.ChartTheme = MaudeChartTheme.Dark),
            BuildButton("Create Test Annotation", () => MaudeRuntime.Event("Test Annotation")),
        };

        var stack = new UIStackView(buttons)
        {
            Axis = UILayoutConstraintAxis.Vertical,
            Distribution = UIStackViewDistribution.FillEqually,
            Alignment = UIStackViewAlignment.Fill,
            Frame = View.Bounds,
            Spacing = 8
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
