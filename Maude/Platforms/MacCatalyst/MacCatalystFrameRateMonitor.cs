#if MACCATALYST
using System;
using CoreAnimation;
using Foundation;
using UIKit;

namespace Maude;

public sealed class MacCatalystFrameRateMonitor : NSObject, IFrameRateMonitor
{
    private const double ReportIntervalSeconds = 0.5d;

    private readonly object sync = new object();
    private CADisplayLink? displayLink;
    private NSObject? didEnterBackgroundObserver;
    private NSObject? didBecomeActiveObserver;
    private double lastReportTimestamp;
    private int framesSinceLastReport;
    private int lastReportedFps;
    private bool running;

    public void Start()
    {
        if (running)
        {
            return;
        }

        running = true;
        lastReportTimestamp = 0;
        framesSinceLastReport = 0;

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            EnsureDisplayLink();
        });

        didEnterBackgroundObserver ??= UIApplication.Notifications.ObserveDidEnterBackground((_, _) =>
        {
            UIApplication.SharedApplication.InvokeOnMainThread(PauseDisplayLink);
        });

        didBecomeActiveObserver ??= UIApplication.Notifications.ObserveDidBecomeActive((_, _) =>
        {
            UIApplication.SharedApplication.InvokeOnMainThread(EnsureDisplayLink);
        });
    }

    public void Stop()
    {
        if (!running)
        {
            return;
        }

        running = false;

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            TearDownDisplayLink();
            lock (sync)
            {
                lastReportedFps = 0;
                framesSinceLastReport = 0;
                lastReportTimestamp = 0;
            }
        });

        didEnterBackgroundObserver?.Dispose();
        didEnterBackgroundObserver = null;
        didBecomeActiveObserver?.Dispose();
        didBecomeActiveObserver = null;
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

    private void OnTick()
    {
        if (displayLink == null || !running)
        {
            return;
        }

        var timestamp = displayLink.Timestamp;
        if (lastReportTimestamp <= 0)
        {
            lastReportTimestamp = timestamp;
            framesSinceLastReport = 0;
            return;
        }

        framesSinceLastReport++;
        var elapsedSeconds = timestamp - lastReportTimestamp;
        if (elapsedSeconds >= ReportIntervalSeconds)
        {
            var fps = (int)Math.Round(framesSinceLastReport / elapsedSeconds);
            lock (sync)
            {
                lastReportedFps = fps;
            }

            framesSinceLastReport = 0;
            lastReportTimestamp = timestamp;
        }
    }

    protected override void Dispose(bool disposing)
    {
        Stop();
        base.Dispose(disposing);
    }

    private void EnsureDisplayLink()
    {
        if (!running)
        {
            return;
        }

        if (displayLink == null)
        {
            displayLink = CADisplayLink.Create(OnTick);
            displayLink.PreferredFramesPerSecond = 0; // use device default
            displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
        }

        displayLink.Paused = false;
    }

    private void PauseDisplayLink()
    {
        if (displayLink != null)
        {
            displayLink.Paused = true;
        }
    }

    private void TearDownDisplayLink()
    {
        if (displayLink != null)
        {
            displayLink.Invalidate();
            displayLink.Dispose();
            displayLink = null;
        }
    }
}
#endif
