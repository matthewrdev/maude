using System;
using System.Threading.Tasks;

namespace Maude;

/// <summary>
/// Encapsulates a user-configured save snapshot action rendered in the slide sheet.
/// </summary>
public sealed class MaudeSaveSnapshotAction
{
    public MaudeSaveSnapshotAction(string label, Func<MaudeSnapshot, Task> copyDelegate)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        }

        Label = label;
        CopyDelegate = copyDelegate ?? throw new ArgumentNullException(nameof(copyDelegate));
    }

    /// <summary>
    /// Display label for the action button.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Delegate invoked with the exported snapshot payload.
    /// </summary>
    public Func<MaudeSnapshot, Task> CopyDelegate { get; }
}
