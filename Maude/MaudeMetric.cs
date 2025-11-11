namespace Maude;

public readonly struct MaudeMetric : IComparable<MaudeMetric>
{
    public required ulong Value { get; init; }
    
    public required DateTime CapturedAtUtc { get; init;}
    
    public required byte Channel { get; init; }

    public int CompareTo(MaudeMetric other)
    {
        return DateTime.Compare(CapturedAtUtc, other.CapturedAtUtc);
    }
}