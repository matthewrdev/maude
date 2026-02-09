namespace Maude.TestHarness;

public static class CustomMaudeConfiguration
{
    public const byte CustomMetricChannelId = 96;
    
    public const byte CustomEventChannelId = 128;

    public static readonly List<MaudeChannel> AdditionalChannels = new List<MaudeChannel>
    {
        new MaudeChannel(CustomMetricChannelId, "Custom Metric", new Color(255, 149, 0)),
        new MaudeChannel(CustomEventChannelId, "Custom Events", new Color(50, 173, 230)),
    };
}