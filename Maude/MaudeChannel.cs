namespace Maude;

public class MaudeChannel
{
    public MaudeChannel(byte id, string name, Color color)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        Id = id;
        Name = name;
        Color = color;
    }

    public byte Id { get; }
    
    public string Name { get; }
    
    public Color Color { get; }
}