namespace Maude;

/// <summary>
/// UI-facing projection of a <see cref="MaudeEvent"/> for binding.
/// </summary>
public class MaudeEventDisplay
{
    public string Icon { get; init; } = "";
    public string Label { get; init; } = "";
    public string Details { get; init; } = "";
    public bool HasDetails { get; init; }
    public Color ChannelColor { get; init; } = Colors.WhiteSmoke;
    public string Timestamp { get; init; } = "";
}