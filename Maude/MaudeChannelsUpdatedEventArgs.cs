namespace Maude;

public class MaudeChannelsUpdatedEventArgs : EventArgs
{
    public MaudeChannelsUpdatedEventArgs(IReadOnlyList<MaudeChannel> added, IReadOnlyList<MaudeChannel> removed)
    {
        Added = added ?? throw new ArgumentNullException(nameof(added));
        Removed = removed ?? throw new ArgumentNullException(nameof(removed));
    }

    public IReadOnlyList<MaudeChannel> Added { get; }
    
    public IReadOnlyList<MaudeChannel> Removed { get; }
}