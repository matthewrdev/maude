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

        var presentSheet = new Button(this) { Text = "Present Sheet" };
        presentSheet.Click += (_, _) => MaudeRuntime.PresentSheet();

        var presentOverlay = new Button(this) { Text = "Present Overlay" };
        presentOverlay.Click += (_, _) => MaudeRuntime.PresentOverlay();

        var dismissOverlay = new Button(this) { Text = "Dismiss Overlay" };
        dismissOverlay.Click += (_, _) => MaudeRuntime.DismissOverlay();

        layout.AddView(presentSheet);
        layout.AddView(presentOverlay);
        layout.AddView(dismissOverlay);

        SetContentView(layout);
    }
}
