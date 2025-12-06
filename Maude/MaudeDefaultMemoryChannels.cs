namespace Maude;

using System;

/// <summary>
/// Flags describing which of Maude's built-in memory channels should be created.
/// </summary>
[Flags]
public enum MaudeDefaultMemoryChannels : byte
{
    None = 0,
    ManagedHeap = 1 << 0,
    NativeHeap = 1 << 1,
    ResidentSetSize = 1 << 2,
    PhysicalFootprint = 1 << 3,
    All = ManagedHeap | NativeHeap | ResidentSetSize | PhysicalFootprint,
#if ANDROID
    PlatformDefaults = ManagedHeap | NativeHeap | ResidentSetSize,
#elif IOS
    PlatformDefaults = ManagedHeap | PhysicalFootprint,
#else
    PlatformDefaults = ManagedHeap,
#endif
}
