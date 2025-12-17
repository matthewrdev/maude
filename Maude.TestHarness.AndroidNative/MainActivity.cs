using Android.App;
using Android.OS;
using Android.Widget;

namespace Maude.TestHarness.AndroidNative;

[Activity(Label = "Maude Android Harness", MainLauncher = true, Exported = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var options = MaudeOptions.CreateBuilder()
            .WithPresentationWindowProvider(() => this)
            .Build();

        MaudeRuntime.InitializeAndActivate(options);

        var layout = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };

        layout.AddView(BuildButton("Present Sheet", () => MaudeRuntime.PresentSheet()));
        layout.AddView(BuildButton("Present Overlay", () => MaudeRuntime.PresentOverlay()));
        layout.AddView(BuildButton("Dismiss Overlay", () => MaudeRuntime.DismissOverlay()));
        layout.AddView(BuildButton("Overlay Top-Left", () => MaudeRuntime.PresentOverlay(MaudeOverlayPosition.TopLeft)));
        layout.AddView(BuildButton("Overlay Top-Right", () => MaudeRuntime.PresentOverlay(MaudeOverlayPosition.TopRight)));
        layout.AddView(BuildButton("Overlay Bottom-Left", () => MaudeRuntime.PresentOverlay(MaudeOverlayPosition.BottomLeft)));
        layout.AddView(BuildButton("Overlay Bottom-Right", () => MaudeRuntime.PresentOverlay(MaudeOverlayPosition.BottomRight)));
        layout.AddView(BuildButton("Theme: Light", () => MaudeRuntime.ChartTheme = MaudeChartTheme.Light));
        layout.AddView(BuildButton("Theme: Dark", () => MaudeRuntime.ChartTheme = MaudeChartTheme.Dark));
        SetContentView(layout);
    }

    private Button BuildButton(string text, Action action)
    {
        var button = new Button(this) { Text = text };
        button.Click += (_, _) => action();
        return button;
    }
}
