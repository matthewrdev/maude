using System.Collections.Generic;

namespace Maude;

/// <summary>
/// Built-in event categories supported by Maude.
/// </summary>
public enum MaudeEventType
{
    Event,
    Debug,
    Info,
    Warning,
    Error,
    Exception,
    Gc,
    Navigation
}

/// <summary>
/// Provides a compact symbol per <see cref="MaudeEventType"/> for rendering events.
/// </summary>
public static class MaudeEventLegend
{
    public static readonly IReadOnlyDictionary<MaudeEventType, string?> Symbols =
        new Dictionary<MaudeEventType, string?>
        {
            { MaudeEventType.Event,      "*" },
            { MaudeEventType.Debug,      "#" },
            { MaudeEventType.Info,       "i" },
            { MaudeEventType.Warning,    "w" },
            { MaudeEventType.Error,      "e" },
            { MaudeEventType.Exception,  "!" },
            { MaudeEventType.Gc,         "g" },
            { MaudeEventType.Navigation, ">" }
        };

    public static string? GetSymbol(MaudeEventType type)
        => Symbols.GetValueOrDefault(type, "?");

    public static bool TryGetSymbol(MaudeEventType type, out string? symbol)
        => Symbols.TryGetValue(type, out symbol);
}
