namespace Maude;

internal class BinarySearchHelper
{
    
    public static int FindFirstIndex(List<MaudeEvent> events, DateTime fromUtc)
    {
        int lo = 0;
        int hi = events.Count - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (events[mid].CapturedAtUtc < fromUtc)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        return lo; // may return events.Count if all < fromUtc
    }
    
    public static int FindLastIndex(List<MaudeEvent> events, DateTime toUtc)
    {
        int lo = 0;
        int hi = events.Count - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (events[mid].CapturedAtUtc > toUtc)
                hi = mid - 1;
            else
                lo = mid + 1;
        }

        return hi; // may return -1 if all > toUtc
    }
    
    public static int FindFirstIndex(List<MaudeMetric> metrics, DateTime fromUtc)
    {
        int lo = 0;
        int hi = metrics.Count - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (metrics[mid].CapturedAtUtc < fromUtc)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        return lo; // may return events.Count if all < fromUtc
    }
    
    public static int FindLastIndex(List<MaudeMetric> metrics, DateTime toUtc)
    {
        int lo = 0;
        int hi = metrics.Count - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (metrics[mid].CapturedAtUtc > toUtc)
                hi = mid - 1;
            else
                lo = mid + 1;
        }

        return hi; // may return -1 if all > toUtc
    }

}
