#if ANDROID || IOS
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;

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
    }
}

internal sealed class MaudeChartOverlayElement : IWindowOverlayElement
{
    private readonly IMaudeDataSink dataSink;
    private readonly TimeSpan windowDuration = TimeSpan.FromMinutes(1);

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
        if (canvas == null) return;

        var overlayWidth = Math.Min(360f, dirtyRect.Width - 24);
        var overlayHeight = Math.Min(240f, dirtyRect.Height - 24);
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

        var now = DateTime.UtcNow;
        var fromUtc = now - windowDuration;
        var toUtc = now;

        var channelLookup = sink.Channels?.ToDictionary(c => c.Id) ?? new Dictionary<byte, MaudeChannel>();

        var metricsByChannel = new Dictionary<byte, IReadOnlyList<MaudeMetric>>();
        double maxValue = 0;

        foreach (var channel in channelLookup.Values)
        {
            var metrics = sink.GetMetricsForChannelInRange(channel.Id, fromUtc, toUtc);
            if (metrics.Count == 0) continue;

            metricsByChannel[channel.Id] = metrics;
            var channelMax = metrics.Max(m => m.Value);
            if (channelMax > maxValue) maxValue = channelMax;
        }

        if (metricsByChannel.Count == 0)
        {
            DrawEmpty(canvas, bounds);
            return;
        }

        if (maxValue <= 0) maxValue = 1;
        var maxDisplayValue = Math.Max(1d, maxValue * 1.1d);

        // Layout metrics
        var gridLines = 4;
        var legendChannels = metricsByChannel.Keys
            .Select(id => channelLookup.TryGetValue(id, out var c) ? c : null)
            .OfType<MaudeChannel>()
            .ToList();

        var legendHeight = legendChannels.Count * 16f;
        var topPadding = 20f + (legendHeight > 0 ? legendHeight + 6f : 0f);
        var leftPadding = 64f;
        var rightPadding = 12f;
        var bottomPadding = 36f;

        var chartRect = new RectF(bounds.X + leftPadding,
                                  bounds.Y + topPadding,
                                  bounds.Width - leftPadding - rightPadding,
                                  bounds.Height - topPadding - bottomPadding);

        if (chartRect.Width <= 0 || chartRect.Height <= 0)
        {
            DrawEmpty(canvas, bounds);
            return;
        }

        // Legend (only channels with metrics)
        canvas.FontSize = 12;
        canvas.FontColor = new Color(220, 220, 230);
        var legendY = bounds.Y + 16 + 12;
        foreach (var channel in legendChannels)
        {
            var channelColor = channel.Color == default ? new Color(123, 97, 255) : channel.Color;
            canvas.FillColor = channelColor;
            canvas.FillCircle(bounds.X + 14, legendY - (12 / 3f), 4);

            canvas.FontColor = new Color(220, 220, 230);
            canvas.DrawString(channel.Name, bounds.X + 24, legendY, HorizontalAlignment.Left);
            legendY += 16;
        }

        // Axes
        canvas.StrokeColor = new Color(80, 80, 95);
        canvas.StrokeSize = 2;
        canvas.DrawLine(chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom);
        canvas.DrawLine(chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom);

        // Grid + labels
        canvas.StrokeColor = new Color(50, 50, 60);
        canvas.StrokeSize = 1;
        canvas.FontSize = 12;
        canvas.FontColor = new Color(210, 210, 224);

        var totalMilliseconds = Math.Max(1d, (toUtc - fromUtc).TotalMilliseconds);

        for (int i = 0; i <= gridLines; i++)
        {
            var y = chartRect.Top + (chartRect.Height / gridLines) * i;
            canvas.DrawLine(chartRect.Left, y, chartRect.Right, y);

            var valueRatio = 1f - (float)i / gridLines;
            var value = (long)Math.Ceiling(maxDisplayValue * valueRatio);
            var label = MaudeChartRenderer.FormatBytes(value);
            canvas.DrawString(label, chartRect.Left - 8, y + 4, HorizontalAlignment.Right);
        }

        // Time labels
        var startLabel = fromUtc.ToLocalTime().ToString("HH:mm:ss");
        var endLabel = toUtc.ToLocalTime().ToString("HH:mm:ss");
        canvas.DrawString(startLabel, chartRect.Left, chartRect.Bottom + 18, HorizontalAlignment.Left);
        canvas.DrawString(endLabel, chartRect.Right, chartRect.Bottom + 18, HorizontalAlignment.Right);

        // Lines
        foreach (var kv in metricsByChannel)
        {
            var channel = channelLookup.TryGetValue(kv.Key, out var c) ? c : new MaudeChannel(kv.Key, $"Channel {kv.Key}", Colors.Purple);
            var channelColor = channel.Color == default ? new Color(123, 97, 255) : channel.Color;

            canvas.StrokeColor = channelColor;
            canvas.StrokeSize = 3;
            canvas.FillColor = channelColor;

            var path = new PathF();
            var first = true;
            foreach (var metric in kv.Value)
            {
                var x = chartRect.Left + (float)((metric.CapturedAtUtc - fromUtc).TotalMilliseconds / totalMilliseconds) * chartRect.Width;
                var y = chartRect.Bottom - (float)(metric.Value / maxDisplayValue) * chartRect.Height;

                if (first)
                {
                    path.MoveTo(x, y);
                    first = false;
                }
                else
                {
                    path.LineTo(x, y);
                }

                canvas.FillCircle(x, y, 2.5f);
            }

            canvas.DrawPath(path);
        }
    }

    private void DrawEmpty(ICanvas canvas, RectF bounds)
    {
        canvas.FontSize = 14;
        canvas.FontColor = new Color(200, 200, 210);
        canvas.DrawString("Waiting for samples...", bounds.Center.X, bounds.Center.Y, HorizontalAlignment.Center);
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
