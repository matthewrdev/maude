#if ANDROID
using System;
using Android.Views;

namespace Maude;

internal sealed class AndroidFrameRateMonitor : Java.Lang.Object, IFrameRateMonitor, Choreographer.IFrameCallback
{
    private const double ReportIntervalSeconds = 0.5d;
    
    private readonly object sync = new object();
    private bool running;
    private long lastFrameNanos;
    private int framesSinceLastReport;
    private int lastReportedFps;

    public void DoFrame(long frameTimeNanos)
    {
        if (!running)
        {
            return;
        }

        if (lastFrameNanos == 0)
        {
            lastFrameNanos = frameTimeNanos;
            framesSinceLastReport = 0;
        }
        else
        {
            framesSinceLastReport++;
            var elapsed = frameTimeNanos - lastFrameNanos;
            if (elapsed >= ReportIntervalSeconds * 1_000_000_000L)
            {
                var fps = (int)Math.Round(framesSinceLastReport * 1_000_000_000d / elapsed);
                lock (sync)
                {
                    lastReportedFps = fps;
                }

                framesSinceLastReport = 0;
                lastFrameNanos = frameTimeNanos;
            }
        }

        if (running)
        {
            Choreographer.Instance.PostFrameCallback(this);
        }
    }

    public int ConsumeFramesPerSecond()
    {
        lock (sync)
        {
            var fps = lastReportedFps;
            lastReportedFps = 0;
            return fps;
        }
    }

    public void Start()
    {
        if (running)
        {
            return;
        }

        running = true;
        lastFrameNanos = 0;
        framesSinceLastReport = 0;
        Choreographer.Instance.PostFrameCallback(this);
    }

    public void Stop()
    {
        if (!running)
        {
            return;
        }

        running = false;
        Choreographer.Instance.RemoveFrameCallback(this);
        lock (sync)
        {
            lastReportedFps = 0;
            framesSinceLastReport = 0;
            lastFrameNanos = 0;
        }
    }

    protected override void Dispose(bool disposing)
    {
        Stop();
        base.Dispose(disposing);
    }
}
#endif
