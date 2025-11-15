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
        try
        {
            foreach (var line in File.ReadLines("/proc/self/status"))
            {
                if (line.StartsWith("VmRSS:", StringComparison.Ordinal))
                {
                    var kb = ExtractFirstNumber(line);
                    if (kb.HasValue) return kb.Value * 1024L;
                }
            }
        }
        catch { }
        return 0;
    }

    static long? ExtractFirstNumber(string s)
    {
        var digits = new string(s.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
        if (long.TryParse(digits, out var val)) return val;
        return null;
    }

#elif IOS
    public static MemorySnapshot Sample()
    {
        // --- RSS via mach_task_basic_info ---
        var rssBytes = IosMemoryHelper.GetPhysFootprint();

        // iOS has no Java/ART or Android-style native heap counters.
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
    public static Snapshot Sample()
    {
        long managedBytes = GC.GetTotalMemory(false);

        return new Snapshot(
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
