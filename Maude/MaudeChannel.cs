namespace Maude;

/// <summary>
/// Defines a metric/event channel rendered by Maude, including its display name and colour.
/// </summary>
public class MaudeChannel
{
    public MaudeChannel(byte id, string name, Color color)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        Id = id;
        Name = name;
        Color = color;
    }

    /// <summary>
    /// Numeric identifier for the channel; reserved IDs are defined in <see cref="MaudeConstants.ReservedChannels"/>.
    /// </summary>
    public byte Id { get; }
    
    /// <summary>
    /// Human-readable channel label shown in the UI.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Colour used when rendering the channel.
    /// </summary>
    public Color Color { get; }
}
