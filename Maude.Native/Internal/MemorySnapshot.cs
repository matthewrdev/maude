namespace Maude;

internal sealed record MemorySnapshot(
    long TotalPssBytes,            // Android: PSS; iOS: 0 (not available)
    long RssBytes,                 // Resident set size (Android via /proc, iOS via mach)
    long JavaHeapUsedBytes,        // Android only; iOS: 0
    long JavaHeapMaxBytes,         // Android only; iOS: 0
    long NativeHeapAllocatedBytes, // Android only; iOS: 0
    long ManagedHeapBytes,         // .NET GC managed heap (approx)
    DateTime CapturedAtUtc);