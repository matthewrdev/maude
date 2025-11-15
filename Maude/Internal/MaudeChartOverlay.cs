#if ANDROID || IOS
using System;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Platform;
using SkiaSharp;

namespace Maude;

internal sealed class MaudeChartWindowOverlay : WindowOverlay, IDisposable
{
    private readonly IMaudeDataSink dataSink;
    private readonly MaudeChartOverlayElement chartElement;

    public MaudeChartWindowOverlay(Window window, IMaudeDataSink sink, MaudeOverlayPosition position) : base(window)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(sink);

        dataSink = sink;
        chartElement = new MaudeChartOverlayElement(sink, position);

        AddWindowElement(chartElement);
        EnableDrawableTouchHandling = false;

        dataSink.OnMetricsUpdated += HandleSinkUpdated;
        dataSink.OnEventsUpdated += HandleSinkUpdated;
    }

    public void UpdatePosition(MaudeOverlayPosition position)
    {
        chartElement.Position = position;
        InvalidateOnMainThread();
    }

    public void RequestRedraw() => InvalidateOnMainThread();

    private void HandleSinkUpdated(object? sender, EventArgs e)
    {
        InvalidateOnMainThread();
    }

    private void InvalidateOnMainThread()
    {
        if (MainThread.IsMainThread)
        {
            Invalidate();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(Invalidate);
        }
    }

    public void Dispose()
    {
        dataSink.OnMetricsUpdated -= HandleSinkUpdated;
        dataSink.OnEventsUpdated -= HandleSinkUpdated;

        RemoveWindowElement(chartElement);
        chartElement.Dispose();
    }
}

internal sealed class MaudeChartOverlayElement : IWindowOverlayElement, IDisposable
{
    private readonly IMaudeDataSink dataSink;
    private readonly TimeSpan windowDuration = TimeSpan.FromMinutes(1);
    private const float OverlaySizeScale = 0.5f;
    private const float OverlayAlpha = 0.85f;
    private SKBitmap? renderBitmap;
    private SKCanvas? renderCanvas;
    private SKImageInfo renderInfo;
    private Microsoft.Maui.Graphics.IImage? renderedFrame;

    private RectF contentBounds;

    public MaudeOverlayPosition Position { get; set; }

    public MaudeChartOverlayElement(IMaudeDataSink dataSink, MaudeOverlayPosition position)
    {
        this.dataSink = dataSink ?? throw new ArgumentNullException(nameof(dataSink));
        Position = position;
    }

    public bool InvalidatesOnInput => false;

    public bool ContainsPoint(Point point) => contentBounds.Contains(point);

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        try
        {
            if (canvas == null)
            {
                return;
            }

            var overlayWidth = Math.Max(0f, Math.Min(360f, dirtyRect.Width - 24)) * OverlaySizeScale;
            var overlayHeight = Math.Max(0f, Math.Min(240f, dirtyRect.Height - 24)) * OverlaySizeScale;
            var margin = 16f;

            var x = Position switch
            {
                MaudeOverlayPosition.TopLeft => margin,
                MaudeOverlayPosition.TopRight => dirtyRect.Width - overlayWidth - margin,
                MaudeOverlayPosition.BottomLeft => margin,
                _ => dirtyRect.Width - overlayWidth - margin
            };

            var y = Position switch
            {
                MaudeOverlayPosition.TopLeft => margin,
                MaudeOverlayPosition.TopRight => margin,
                MaudeOverlayPosition.BottomLeft => dirtyRect.Height - overlayHeight - margin,
                _ => dirtyRect.Height - overlayHeight - margin
            };

            contentBounds = new RectF(x, y, overlayWidth, overlayHeight);

            canvas.SaveState();

            // Outer dim background (mostly transparent)
            canvas.FillColor = new Color(0, 0, 0, 0.15f);
            canvas.FillRoundedRectangle(contentBounds, 12);

            var innerBounds = new RectF(contentBounds.X + 10, contentBounds.Y + 10, contentBounds.Width - 20, contentBounds.Height - 20);

            // Card background with translucency
            canvas.FillColor = new Color(18, 18, 26, 0.75f);
            canvas.FillRoundedRectangle(innerBounds, 10);

            canvas.StrokeSize = 1;
            canvas.StrokeColor = new Color(80, 80, 90, 0.8f);
            canvas.DrawRoundedRectangle(innerBounds, 10);

            DrawChart(canvas, innerBounds);

            canvas.RestoreState();
        }
        catch (Exception e)
        {
            MaudeLogger.Exception(e);
        }
    }

    public bool Contains(Point point)
    {
        return false;
    }

    private void DrawChart(ICanvas canvas, RectF bounds)
    {
        var sink = dataSink;
        if (sink == null)
        {
            DrawEmpty(canvas, bounds);
            return;
        }

        var bufferWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width));
        var bufferHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height));
        if (!EnsureRenderBuffer(bufferWidth, bufferHeight) || renderCanvas == null)
        {
            DrawEmpty(canvas, bounds);
            return;
        }

        var now = DateTime.UtcNow;
        var channels = sink.Channels?.Select(c => c.Id).ToArray() ?? Array.Empty<byte>();
        var renderOptions = new MaudeRenderOptions
        {
            Channels = channels,
            FromUtc = now - windowDuration,
            ToUtc = now,
            CurrentUtc = now,
            Mode = MaudeChartRenderMode.Overlay,
            ProbePosition = null
        };

        MaudeChartRenderer.Render(renderCanvas, renderInfo, sink, renderOptions);

        if (!TryUpdatePlatformImage())
        {
            DrawEmpty(canvas, bounds);
            return;
        }

        var clipPath = new PathF();
        clipPath.AppendRoundedRectangle(bounds, 10);
        canvas.SaveState();
        canvas.ClipPath(clipPath, WindingMode.NonZero);
        canvas.SaveState();
        canvas.Alpha = OverlayAlpha;
        if (renderedFrame != null)
        {
            canvas.DrawImage(renderedFrame, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }
        canvas.RestoreState();
        canvas.RestoreState();
    }

    private void DrawEmpty(ICanvas canvas, RectF bounds)
    {
        canvas.FontSize = 14;
        canvas.FontColor = new Color(200, 200, 210);
        canvas.DrawString("Waiting for samples...", bounds.Center.X, bounds.Center.Y, HorizontalAlignment.Center);
    }

    private bool EnsureRenderBuffer(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        if (renderBitmap != null && (renderBitmap.Width != width || renderBitmap.Height != height))
        {
            ReleaseRenderBuffer();
        }

        if (renderBitmap == null)
        {
            renderBitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            renderCanvas = new SKCanvas(renderBitmap);
            renderInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        }

        return renderBitmap != null && renderCanvas != null;
    }

    private void ReleaseRenderBuffer()
    {
        renderCanvas?.Dispose();
        renderCanvas = null;
        renderBitmap?.Dispose();
        renderBitmap = null;
        renderedFrame?.Dispose();
        renderedFrame = null;
        renderInfo = default;
    }

    private bool TryUpdatePlatformImage()
    {
        if (renderBitmap == null)
        {
            return false;
        }

        using var snapshot = SKImage.FromBitmap(renderBitmap);
        if (snapshot == null)
        {
            return false;
        }

        using var data = snapshot.Encode(SKEncodedImageFormat.Png, 90);
        if (data == null)
        {
            return false;
        }

        using var stream = data.AsStream();
        var frame = PlatformImage.FromStream(stream, ImageFormat.Png);
        if (frame == null)
        {
            return false;
        }

        renderedFrame?.Dispose();
        renderedFrame = frame;
        return true;
    }

    public void Dispose()
    {
        ReleaseRenderBuffer();
    }
}

internal static class WindowOverlayHelpers
{
    public static bool TryAddOverlay(Window? window, WindowOverlay overlay)
    {
        if (window == null) return false;

        // Prefer official API if present
        var addOverlay = window.GetType().GetMethod("AddOverlay", new[] { typeof(WindowOverlay) });
        if (addOverlay != null)
        {
            addOverlay.Invoke(window, new object[] { overlay });
            return true;
        }

        var handler = window.Handler;
        var addMethod = handler?.GetType().GetMethod("AddOverlay", new[] { typeof(WindowOverlay) });
        if (addMethod != null)
        {
            addMethod.Invoke(handler, new object[] { overlay });
            return true;
        }

        return false;
    }

    public static bool TryRemoveOverlay(Window? window, WindowOverlay overlay)
    {
        if (window == null) return false;

        var removeOverlay = window.GetType().GetMethod("RemoveOverlay", new[] { typeof(WindowOverlay) });
        if (removeOverlay != null)
        {
            removeOverlay.Invoke(window, new object[] { overlay });
            return true;
        }

        var handler = window.Handler;
        var removeMethod = handler?.GetType().GetMethod("RemoveOverlay", new[] { typeof(WindowOverlay) });
        if (removeMethod != null)
        {
            removeMethod.Invoke(handler, new object[] { overlay });
            return true;
        }

        return false;
    }
}
#endif
