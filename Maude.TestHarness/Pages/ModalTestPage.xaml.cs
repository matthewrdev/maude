namespace Maude.TestHarness;

public partial class ModalTestPage : ContentPage
{
    public ModalTestPage()
    {
        InitializeComponent();
    }

    private async void OnDismissClicked(object? sender, EventArgs e)
    {
        if (Navigation == null)
        {
            return;
        }

        MaudeRuntime.Event("Pop ModalTestPage", CustomMaudeConfiguration.CustomEventChannelId);
        await Navigation.PopModalAsync();
    }
}
