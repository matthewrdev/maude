#if IOS
using System;
using CoreAnimation;
using Foundation;
using Microsoft.Maui.ApplicationModel;

namespace Maude;

internal sealed class IosFrameRateMonitor : NSObject, IFrameRateMonitor
{
    private const double ReportIntervalSeconds = 0.5d;
    
    private readonly object sync = new object();
    private CADisplayLink? displayLink;
    private double lastReportTimestamp;
    private int framesSinceLastReport;
    private int lastReportedFps;

    public void Start()
    {
        if (displayLink != null)
        {
            return;
        }

        lastReportTimestamp = 0;
        framesSinceLastReport = 0;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            displayLink = CADisplayLink.Create(OnTick);
            displayLink.PreferredFramesPerSecond = 0; // use device default
            displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
        });
    }

    public void Stop()
    {
        if (displayLink == null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            displayLink?.Invalidate();
            displayLink?.Dispose();
            displayLink = null;
            lock (sync)
            {
                lastReportedFps = 0;
                framesSinceLastReport = 0;
                lastReportTimestamp = 0;
            }
        });
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
        if (displayLink == null)
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
}
#endif
