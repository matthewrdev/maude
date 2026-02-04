namespace Maude;

/// <summary>
/// Represents a single sampled metric value captured by Maude.
/// </summary>
public class MaudeMetric : IComparable<MaudeMetric>
{
    public required long Value { get; init; }
    
    public required DateTime CapturedAtUtc { get; init;}
    
    public required byte Channel { get; init; }

    public int CompareTo(MaudeMetric? other)
    {
        if (other is null)
        {
            return 1;
        }
        
        return DateTime.Compare(CapturedAtUtc, other.CapturedAtUtc);
    }
}
