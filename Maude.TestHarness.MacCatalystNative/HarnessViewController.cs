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
            Spacing = 8
        };

        var scrollView = new UIScrollView();
        scrollView.TranslatesAutoresizingMaskIntoConstraints = false;
        stack.TranslatesAutoresizingMaskIntoConstraints = false;

        scrollView.AddSubview(stack);
        View.AddSubview(scrollView);

        NSLayoutConstraint.ActivateConstraints(new[]
        {
            scrollView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
            scrollView.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor),
            scrollView.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor),
            scrollView.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor),

            stack.TopAnchor.ConstraintEqualTo(scrollView.ContentLayoutGuide.TopAnchor),
            stack.BottomAnchor.ConstraintEqualTo(scrollView.ContentLayoutGuide.BottomAnchor),
            stack.LeadingAnchor.ConstraintEqualTo(scrollView.ContentLayoutGuide.LeadingAnchor),
            stack.TrailingAnchor.ConstraintEqualTo(scrollView.ContentLayoutGuide.TrailingAnchor),
            stack.WidthAnchor.ConstraintEqualTo(scrollView.FrameLayoutGuide.WidthAnchor)
        });
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
