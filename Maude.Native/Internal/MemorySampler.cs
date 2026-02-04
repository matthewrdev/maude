using System.Runtime.InteropServices;

namespace Maude;

internal static class MemorySampler
{

#if ANDROID
    public static MemorySnapshot Sample()
    {
        var ctx = global::Android.App.Application.Context!;
        var am = (global::Android.App.ActivityManager)ctx.GetSystemService(global::Android.Content.Context.ActivityService)!;
        int pid = global::Android.OS.Process.MyPid();
        var memInfo = am.GetProcessMemoryInfo(new[] { pid })[0];
#pragma warning disable CA1422
        long totalPssBytes = (long)memInfo.TotalPss * 1024L;
#pragma warning restore CA1422

        long rssBytes = TryReadProcStatusRssBytes();

        var rt = global::Java.Lang.Runtime.GetRuntime();
        long javaUsedBytes = (long)(rt.TotalMemory() - rt.FreeMemory());
        long javaMaxBytes  = (long)rt.MaxMemory();

        long nativeAllocatedBytes = global::Android.OS.Debug.NativeHeapAllocatedSize;

        long managedBytes = GC.GetTotalMemory(false);

        return new MemorySnapshot(
            TotalPssBytes: totalPssBytes,
            RssBytes: rssBytes,
            JavaHeapUsedBytes: javaUsedBytes,
            JavaHeapMaxBytes: javaMaxBytes,
            NativeHeapAllocatedBytes: nativeAllocatedBytes,
            ManagedHeapBytes: managedBytes,
            CapturedAtUtc: DateTime.UtcNow);
    }

    static long TryReadProcStatusRssBytes()
    {
        // /proc/self/status is small; read once into a stack buffer and scan for "VmRSS".
        Span<byte> buffer = stackalloc byte[2048];
        try
        {
            using var fs = new FileStream("/proc/self/status", FileMode.Open, FileAccess.Read, FileShare.ReadWrite, buffer.Length, FileOptions.None);
            var read = fs.Read(buffer);
            if (read <= 0) return 0;

            var span = buffer.Slice(0, read);
            ReadOnlySpan<byte> key = "VmRSS:"u8;

            for (int i = 0; i <= span.Length - key.Length; i++)
            {
                if (!span.Slice(i, key.Length).SequenceEqual(key))
                {
                    // fast-forward until the next potential 'V' to reduce comparisons
                    while (i + 1 < span.Length && span[i + 1] != (byte)'V')
                    {
                        i++;
                    }
                    continue;
                }

                var j = i + key.Length;
                while (j < span.Length && (span[j] == (byte)' ' || span[j] == (byte)'\t')) j++;

                long valueKb = 0;
                while (j < span.Length && span[j] >= (byte)'0' && span[j] <= (byte)'9')
                {
                    valueKb = (valueKb * 10) + (span[j] - (byte)'0');
                    j++;
                }

                return valueKb * 1024L;
            }

            return 0;
        }
        catch { }
        return 0;
    }

#elif IOS || MACCATALYST
    public static MemorySnapshot Sample()
    {
        // --- RSS via mach_task_basic_info ---
        var rssBytes = IosMemoryHelper.GetPhysFootprint();

        // iOS/macOS have no Java/ART or Android-style native heap counters.
        long managedBytes = GC.GetTotalMemory(false);

        return new MemorySnapshot(
            TotalPssBytes: 0,                 // PSS not available on iOS
            RssBytes: (long)rssBytes,               // Resident set size (includes shared pages)
            JavaHeapUsedBytes: 0,
            JavaHeapMaxBytes: 0,
            NativeHeapAllocatedBytes: 0,
            ManagedHeapBytes: managedBytes,
            CapturedAtUtc: DateTime.UtcNow);
    }
#else
    public static MemorySnapshot Sample()
    {
        long managedBytes = GC.GetTotalMemory(false);

        return new MemorySnapshot(
            TotalPssBytes: 0,
            RssBytes: 0,
            JavaHeapUsedBytes: 0,
            JavaHeapMaxBytes: 0,
            NativeHeapAllocatedBytes: 0,
            ManagedHeapBytes: managedBytes,
            CapturedAtUtc: DateTime.UtcNow);
    }
#endif

    // Convenience: human-readable formatting for logs
    public static string Describe(MemorySnapshot s) =>
        $"[{s.CapturedAtUtc:O}] " +
        (s.TotalPssBytes > 0 ? $"PSS={ToMB(s.TotalPssBytes)} MB, " : "") +
        $"RSS={ToMB(s.RssBytes)} MB, " +
        (s.JavaHeapMaxBytes > 0 ? $"JavaUsed={ToMB(s.JavaHeapUsedBytes)} MB / JavaMax={ToMB(s.JavaHeapMaxBytes)} MB, " : "") +
        (s.NativeHeapAllocatedBytes > 0 ? $"NativeAllocated={ToMB(s.NativeHeapAllocatedBytes)} MB, " : "") +
        $"Managed={ToMB(s.ManagedHeapBytes)} MB";

    static string ToMB(long bytes) => ((bytes / 1024d) / 1024d).ToString("0.##");
}
