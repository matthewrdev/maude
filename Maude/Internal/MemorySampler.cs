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
        long rssBytes = GetResidentSizeBytes();

        // iOS has no Java/ART or Android-style native heap counters.
        long managedBytes = GC.GetTotalMemory(false);

        return new MemorySnapshot(
            TotalPssBytes: 0,                 // PSS not available on iOS
            RssBytes: rssBytes,               // Resident set size (includes shared pages)
            JavaHeapUsedBytes: 0,
            JavaHeapMaxBytes: 0,
            NativeHeapAllocatedBytes: 0,
            ManagedHeapBytes: managedBytes,
            CapturedAtUtc: DateTime.UtcNow);
    }

    static long GetResidentSizeBytes()
    {
        var info = new mach_task_basic_info();
        uint count = MACH_TASK_BASIC_INFO_COUNT;

        // KERN_SUCCESS == 0
        int kerr = task_info(mach_task_self(), MACH_TASK_BASIC_INFO, ref info, ref count);
        if (kerr == 0)
        {
            // resident_size is a pointer-sized integer; convert safely.
            return info.resident_size.ToInt64();
        }
        return 0;
    }

    // ---- mach imports ----
    const int MACH_TASK_BASIC_INFO = 20;

    // task_info expects "count" in 32-bit "natural_t" units; compute as sizeof(struct)/sizeof(uint)
    static readonly uint MACH_TASK_BASIC_INFO_COUNT = (uint)(Marshal.SizeOf<mach_task_basic_info>() / sizeof(uint));

    [DllImport("/usr/lib/libSystem.dylib")]
    static extern int task_info(IntPtr target_task, int flavor, ref mach_task_basic_info task_info_out, ref uint task_info_outCnt);

    [DllImport("/usr/lib/libSystem.dylib")]
    static extern IntPtr mach_task_self();

    // Matches the Darwin definition; fields sized for 64-bit iOS
    [StructLayout(LayoutKind.Sequential)]
    struct time_value_t
    {
        public int seconds;
        public int microseconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct mach_task_basic_info
    {
        public IntPtr virtual_size;       // mach_vm_size_t
        public IntPtr resident_size;      // mach_vm_size_t
        public IntPtr resident_size_max;  // mach_vm_size_t
        public time_value_t user_time;    // time_value_t
        public time_value_t system_time;  // time_value_t
        public int policy;                // policy_t
        public int suspend_count;         // integer_t
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
