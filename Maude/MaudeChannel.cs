namespace Maude;

public struct MaudeChannel
{
    public required byte Id { get; init; }
    
    public required string Name { get; init; }
    
    public required Color Color { get; init; }
}