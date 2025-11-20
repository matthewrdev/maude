using System;
using System.Runtime.CompilerServices;

namespace Maude;

internal sealed class MemorySamplerThread : IDisposable
{
    private readonly int intervalMs;
    private readonly Action<MemorySnapshot> onSampleCaptured;

    private readonly ManualResetEvent exitEvent = new(initialState: false);
    private readonly AutoResetEvent triggerEvent = new(initialState: false);
    private readonly WaitHandle[] waitHandles;

    private readonly Thread workerThread;
    private int started;               // 0 = not started, 1 = started
    private int stopping;              // 0 = running, 1 = stopping
    private bool disposed;


    public MemorySamplerThread(int intervalMilliseconds, Action<MemorySnapshot> onSampleCaptured)
    {
        if (intervalMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds));

        this.intervalMs = intervalMilliseconds;
        this.onSampleCaptured = onSampleCaptured ?? throw new ArgumentNullException(nameof(onSampleCaptured));
        
        waitHandles = new WaitHandle[] { exitEvent, triggerEvent };

        workerThread = new Thread(ThreadRun)
        {
            IsBackground = true,
            Name = $"{nameof(MemorySamplerThread)}",
        };

        StartIfNeeded();
    }

    /// <summary>
    /// Immediately triggers a sample outside of the regular tick.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TriggerNow() => triggerEvent.Set();

    /// <summary>
    /// Requests the background thread to stop and waits for it to finish.
    /// </summary>
    public void Stop()
    {
        if (Interlocked.Exchange(ref stopping, 1) == 1) return;
        try
        {
            exitEvent.Set();
            if (Thread.CurrentThread != workerThread && workerThread.IsAlive)
            {
                // Give it a short, bounded time to exit cleanly.
                workerThread.Join(TimeSpan.FromSeconds(2));
            }
        }
        catch { /* swallow on shutdown */ }
    }

    private void StartIfNeeded()
    {
        if (Interlocked.Exchange(ref started, 1) == 0)
        {
            workerThread.Start();
        }
    }

    private void ThreadRun()
    {
        // We’ll use WaitAny(handles, timeout) pattern:
        //   index 0 = exit, index 1 = immediate trigger, timeout = periodic tick → perform sample
        using var cts = new CancellationTokenSource();
        try
        {
            while (true)
            {
                int signaled = WaitHandle.WaitAny(waitHandles, intervalMs, exitContext: false);

                if (signaled == 0) // exitEvent
                    break;

                // If timed out or trigger event fired, run a sample.
                // (signaled == WaitHandle.WaitTimeout) or (signaled == 1)
                try
                {
                    var snapshot = MemorySampler.Sample();
                    
                    this.onSampleCaptured(snapshot);
                }
                catch (Exception)
                {
                    // Suppressed, should not happen.
                }
            }
        }
        finally
        {
            // ensure we won't re-enter start/stop race
            Interlocked.Exchange(ref started, 0);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Stop();
        exitEvent.Dispose();
        triggerEvent.Dispose();
    }
}
