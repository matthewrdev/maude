#if ANDROID
using System;
using System.Threading;
using Android.Content;
using Android.Views;
using SkiaSharp.Views.Android;
using SkiaSharp;

namespace Maude;

/// <summary>
/// Native Android SKCanvasView-based chart view (no MAUI dependency).
/// </summary>
internal sealed class MaudeNativeChartViewAndroid : SKCanvasView
{
    private IMaudeDataSink? dataSink;
    private Timer? redrawTimer;
    private float? probeRatio;
    private SKRect? chartBounds;
    private float lastCanvasWidth;
    private float lastCanvasHeight;

    public MaudeNativeChartViewAndroid(Context context) : base(context)
    {
        IgnorePixelScaling = true;
        Touch += OnCanvasTouch;
        PaintSurface += OnPaintSurface;
        StartTimer();
    }

    public TimeSpan WindowDuration { get; set; } = TimeSpan.FromMinutes(1);

    public MaudeChartRenderMode RenderMode { get; set; } = MaudeChartRenderMode.Inline;

    public IMaudeDataSink? DataSink
    {
        get => dataSink;
        set
        {
            if (dataSink != null)
            {
                dataSink.OnMetricsUpdated -= HandleMetricsUpdated;
                dataSink.OnEventsUpdated -= HandleEventsUpdated;
            }

            dataSink = value;

            if (dataSink != null)
            {
                dataSink.OnMetricsUpdated += HandleMetricsUpdated;
                dataSink.OnEventsUpdated += HandleEventsUpdated;
            }

            Invalidate();
        }
    }

    private void HandleMetricsUpdated(object? sender, MaudeMetricsUpdatedEventArgs e) => Invalidate();

    private void HandleEventsUpdated(object? sender, MaudeEventsUpdatedEventArgs e) => Invalidate();

    private void StartTimer()
    {
        redrawTimer = new Timer(_ => Invalidate(), null, 500, 500);
    }

    private void OnPaintSurface(object? sender, SkiaSharp.Views.Android.SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        lastCanvasWidth = e.Info.Width;
        lastCanvasHeight = e.Info.Height;
        var sink = dataSink;
        if (sink == null)
        {
            canvas.Clear();
            return;
        }

        var channels = sink.Channels?.Select(c => c.Id).ToArray() ?? Array.Empty<byte>();
        var now = DateTime.UtcNow;
        if (WindowDuration <= TimeSpan.Zero)
        {
            WindowDuration = TimeSpan.FromSeconds(30);
        }

        var renderOptions = new MaudeRenderOptions()
        {
            Channels = channels,
            FromUtc = now - WindowDuration,
            ToUtc = now,
            CurrentUtc = now,
            Mode = RenderMode,
            ProbePosition = probeRatio,
            EventRenderingBehaviour = MaudeRuntime.EventRenderingBehaviour,
            Theme = MaudeRuntime.ChartTheme
        };

        var renderResult = MaudeChartRenderer.Render(canvas, e.Info, sink, renderOptions);

        if (renderResult.HasChartArea && renderResult.ChartBounds.Width > 0 && renderResult.ChartBounds.Height > 0)
        {
            chartBounds = renderResult.ChartBounds;
        }
        else
        {
            chartBounds = null;
        }
    }

    private void OnCanvasTouch(object? sender, TouchEventArgs e)
    {
        switch (e.Event?.Action)
        {
            case MotionEventActions.Down:
            case MotionEventActions.Move:
                var chartRect = chartBounds;
                if (!chartRect.HasValue || chartRect.Value.Width <= 0 || Width == 0)
                {
                    break;
                }

                var scaleX = lastCanvasWidth > 0 ? lastCanvasWidth / Width : 1f;
                var touchX = e.Event.GetX() * scaleX;
                var relativeX = (touchX - chartRect.Value.Left) / chartRect.Value.Width;
                var ratio = (float)Math.Clamp(relativeX, 0d, 1d);
                if (!float.IsNaN(ratio))
                {
                    probeRatio = ratio;
                    e.Handled = true;
                    Invalidate();
                }
                break;
            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                probeRatio = null;
                e.Handled = true;
                Invalidate();
                break;
        }
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (RenderMode == MaudeChartRenderMode.Overlay)
        {
            return false;
        }

        return base.OnTouchEvent(e);
    }

    public void Detach()
    {
        if (redrawTimer != null)
        {
            redrawTimer.Dispose();
            redrawTimer = null;
        }

        if (dataSink != null)
        {
            dataSink.OnMetricsUpdated -= HandleMetricsUpdated;
            dataSink.OnEventsUpdated -= HandleEventsUpdated;
            dataSink = null;
        }

        Touch -= OnCanvasTouch;
        PaintSurface -= OnPaintSurface;
    }
}
#endif
