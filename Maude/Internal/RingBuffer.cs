// MaudeMetricRingBuffer.cs
using System;

namespace Maude;

public sealed class MaudeMetricRingBuffer
{
    private readonly MaudeMetric[] buffer;
    private int _start; // index of the oldest element
    private int _count; // number of valid elements
    private readonly Lock contentsLock = new();

    public int Capacity => buffer.Length;

    public int Count
    {
        get
        {
            lock (contentsLock)
            {
                return _count;
            }
        }
    }

    // Extents tracked live
    public ulong MinValue { get; private set; }
    public ulong MaxValue { get; private set; }
    public DateTime MinCapturedAtUtc { get; private set; }
    public DateTime MaxCapturedAtUtc { get; private set; }

    public MaudeMetricRingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        buffer = new MaudeMetric[capacity];
        _start = 0;
        _count = 0;
        ResetExtents();
    }

    private void ResetExtents()
    {
        MinValue = ulong.MaxValue;
        MaxValue = ulong.MinValue;
        MinCapturedAtUtc = DateTime.MaxValue;
        MaxCapturedAtUtc = DateTime.MinValue;
    }

    /// <summary>
    /// Adds a metric. Enforces that metric.CapturedAtUtc is strictly greater
    /// than current MaxCapturedAtUtc (monotonic), else throws InvalidOperationException.
    /// Overwrites oldest when full. Updates min/max stats.
    /// </summary>
    public void Push(in MaudeMetric metric)
    {
        lock (contentsLock)
        {
            if (_count > 0 && metric.CapturedAtUtc <= MaxCapturedAtUtc)
            {
                throw new InvalidOperationException(
                    $"Out-of-order metric: CapturedAtUtc {metric.CapturedAtUtc:O} must be > {MaxCapturedAtUtc:O}.");
            }

            int writeIndex = (_start + _count) % Capacity;
            bool overwriting = _count == Capacity;
            MaudeMetric overwritten = default;

            if (overwriting)
            {
                overwritten = buffer[writeIndex];
                _start = (_start + 1) % Capacity;
            }
            else
            {
                _count++;
            }

            buffer[writeIndex] = metric;

            if (overwriting &&
                (overwritten.Value == MinValue || overwritten.Value == MaxValue ||
                 overwritten.CapturedAtUtc == MinCapturedAtUtc || overwritten.CapturedAtUtc == MaxCapturedAtUtc))
            {
                RecalculateExtents();
            }
            else
            {
                UpdateExtents(metric);
            }
        }
    }

    /// <summary>
    /// Non-throwing variant of Push. Returns false if the monotonic constraint fails.
    /// </summary>
    public bool TryPush(in MaudeMetric metric)
    {
        lock (contentsLock)
        {
            if (_count > 0 && metric.CapturedAtUtc <= MaxCapturedAtUtc)
                return false;

            int writeIndex = (_start + _count) % Capacity;
            bool overwriting = _count == Capacity;
            MaudeMetric overwritten = default;

            if (overwriting)
            {
                overwritten = buffer[writeIndex];
                _start = (_start + 1) % Capacity;
            }
            else
            {
                _count++;
            }

            buffer[writeIndex] = metric;

            if (overwriting &&
                (overwritten.Value == MinValue || overwritten.Value == MaxValue ||
                 overwritten.CapturedAtUtc == MinCapturedAtUtc || overwritten.CapturedAtUtc == MaxCapturedAtUtc))
            {
                RecalculateExtents();
            }
            else
            {
                UpdateExtents(metric);
            }

            return true;
        }
    }

    private void UpdateExtents(in MaudeMetric metric)
    {
        if (metric.Value < MinValue) MinValue = metric.Value;
        if (metric.Value > MaxValue) MaxValue = metric.Value;
        if (metric.CapturedAtUtc < MinCapturedAtUtc) MinCapturedAtUtc = metric.CapturedAtUtc;
        if (metric.CapturedAtUtc > MaxCapturedAtUtc) MaxCapturedAtUtc = metric.CapturedAtUtc;
    }

    private void RecalculateExtents()
    {
        ResetExtents();
        for (int i = 0; i < _count; i++)
        {
            var m = buffer[(_start + i) % Capacity];
            UpdateExtents(m);
        }
    }

    public void Clear()
    {
        lock (contentsLock)
        {
            _start = 0;
            _count = 0;
            ResetExtents();
            // Optional: Array.Clear(_buffer, 0, _buffer.Length);
        }
    }

    public MaudeMetric this[int index]
    {
        get
        {
            lock (contentsLock)
            {
                if ((uint)index >= (uint)_count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return buffer[(_start + index) % Capacity];
            }
        }
    }

    /// <summary>
    /// Returns a snapshot of all metrics (oldest → newest).
    /// </summary>
    public MaudeMetric[] ToArray()
    {
        lock (contentsLock)
        {
            var n = _count;
            if (n == 0) return Array.Empty<MaudeMetric>();

            var result = new MaudeMetric[n];
            int firstChunk = Math.Min(n, Capacity - _start);
            Array.Copy(buffer, _start, result, 0, firstChunk);
            int remaining = n - firstChunk;
            if (remaining > 0)
                Array.Copy(buffer, 0, result, firstChunk, remaining);
            return result;
        }
    }

    /// <summary>
    /// Copies metrics whose CapturedAtUtc is within [min, max] into 'destination' (oldest → newest).
    /// Returns the number of items copied. Zero-allocation on success.
    /// </summary>
    public int CopyRange(DateTime minCapturedAtUtcInclusive,
                         DateTime maxCapturedAtUtcInclusive,
                         Span<MaudeMetric> destination)
    {
        if (minCapturedAtUtcInclusive > maxCapturedAtUtcInclusive)
            throw new ArgumentException("min must be <= max");

        lock (contentsLock)
        {
            if (_count == 0) return 0;

            // Quick reject via extents
            if (maxCapturedAtUtcInclusive < MinCapturedAtUtc ||
                minCapturedAtUtcInclusive > MaxCapturedAtUtc)
                return 0;

            // lower bound (first idx with time >= min)
            int lo = 0, hi = _count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                var t = buffer[(_start + mid) % Capacity].CapturedAtUtc;
                if (t < minCapturedAtUtcInclusive) lo = mid + 1;
                else hi = mid;
            }
            int startIdx = lo;

            // upper bound (first idx with time > max)
            lo = 0; hi = _count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                var t = buffer[(_start + mid) % Capacity].CapturedAtUtc;
                if (t <= maxCapturedAtUtcInclusive) lo = mid + 1;
                else hi = mid;
            }
            int endExclusive = lo;

            int needed = endExclusive - startIdx;
            if (needed <= 0) return 0;

            int toCopy = Math.Min(needed, destination.Length);
            if (toCopy <= 0) return 0;

            int physStart = (_start + startIdx) % Capacity;

            int firstChunk = Math.Min(toCopy, Capacity - physStart);
            buffer.AsSpan(physStart, firstChunk).CopyTo(destination);
            int remaining = toCopy - firstChunk;
            if (remaining > 0)
            {
                buffer.AsSpan(0, remaining).CopyTo(destination.Slice(firstChunk, remaining));
            }

            return toCopy;
        }
    }

    /// <summary>
    /// Allocates and returns all metrics whose CapturedAtUtc is within [min, max] (oldest → newest).
    /// </summary>
    public MaudeMetric[] ToArray(DateTime minCapturedAtUtcInclusive,
                                 DateTime maxCapturedAtUtcInclusive)
    {
        if (minCapturedAtUtcInclusive > maxCapturedAtUtcInclusive)
            throw new ArgumentException("min must be <= max");

        lock (contentsLock)
        {
            if (_count == 0) return Array.Empty<MaudeMetric>();

            if (maxCapturedAtUtcInclusive < MinCapturedAtUtc ||
                minCapturedAtUtcInclusive > MaxCapturedAtUtc)
                return Array.Empty<MaudeMetric>();

            int lo = 0, hi = _count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                var t = buffer[(_start + mid) % Capacity].CapturedAtUtc;
                if (t < minCapturedAtUtcInclusive) lo = mid + 1;
                else hi = mid;
            }
            int startIdx = lo;

            lo = 0; hi = _count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                var t = buffer[(_start + mid) % Capacity].CapturedAtUtc;
                if (t <= maxCapturedAtUtcInclusive) lo = mid + 1;
                else hi = mid;
            }
            int endExclusive = lo;

            int count = endExclusive - startIdx;
            if (count <= 0) return Array.Empty<MaudeMetric>();

            var result = new MaudeMetric[count];
            int physStart = (_start + startIdx) % Capacity;

            int firstChunk = Math.Min(count, Capacity - physStart);
            Array.Copy(buffer, physStart, result, 0, firstChunk);
            int remaining = count - firstChunk;
            if (remaining > 0)
            {
                Array.Copy(buffer, 0, result, firstChunk, remaining);
            }

            return result;
        }
    }

    /// <summary>
    /// Copies from another buffer:
    /// - If other.Count ≤ Capacity: copies all (oldest→newest)
    /// - If other.Count  > Capacity: takes the most recent 'Capacity' items
    /// Extents are recomputed after copy.
    /// </summary>
    public void CopyFrom(MaudeMetricRingBuffer other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        if (ReferenceEquals(this, other)) return;

        // Snapshot the source (thread-safe inside ToArray).
        var src = other.ToArray();

        lock (contentsLock)
        {
            int n = Math.Min(Capacity, src.Length);
            if (n == 0)
            {
                Clear();
                return;
            }

            int start = src.Length - n; // take last n (most recent)
            Array.Copy(src, start, buffer, 0, n);
            _start = 0;
            _count = n;
            RecalculateExtents();
        }
    }
}
