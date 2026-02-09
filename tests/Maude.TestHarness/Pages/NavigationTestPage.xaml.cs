namespace Maude.TestHarness;

public partial class NavigationTestPage : ContentPage
{
    public NavigationTestPage()
    {
        InitializeComponent();
    }

    private async void OnPopClicked(object? sender, EventArgs e)
    {
        if (Navigation == null)
        {
            return;
        }

        MaudeRuntime.Event("Pop NavigationTestPage", CustomMaudeConfiguration.CustomEventChannelId);
        await Navigation.PopAsync();
    }
}
