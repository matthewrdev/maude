using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Maude;

public static class MaudeChartRenderer
{
    public static void Render(SKCanvas canvas,
                              SKImageInfo info, 
                              IMaudeDataSink dataSink,
                              MaudeRenderOptions renderOptions)
    {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (dataSink == null) throw new ArgumentNullException(nameof(dataSink));
        if (renderOptions.Channels == null) throw new ArgumentNullException(nameof(renderOptions.Channels));

        canvas.Clear(SKColors.Transparent);

        var bounds = new SKRect(0, 0, info.Width, info.Height);
        using var surfaceBackground = new SKPaint
        {
            Color = new SKColor(18, 18, 26),
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRect(bounds, surfaceBackground);

        var fromUtc = renderOptions.FromUtc;
        var toUtc = renderOptions.ToUtc <= fromUtc
            ? fromUtc.AddMilliseconds(1)
            : renderOptions.ToUtc;

        var visibleChannels = (renderOptions.Channels.Count > 0
            ? renderOptions.Channels
            : dataSink.Channels.Select(c => c.Id)).ToArray();

        var channelLookup = dataSink.Channels?.ToDictionary(c => c.Id) ?? new Dictionary<byte, MaudeChannel>();
        var metricsByChannel = new Dictionary<byte, IReadOnlyList<MaudeMetric>>();
        var eventsByChannel = new Dictionary<byte, IReadOnlyList<MaudeEvent>>();
        double maxValue = 0;

        foreach (var channelId in visibleChannels)
        {
            var metrics = dataSink.GetMetricsForChannelInRange(channelId, fromUtc, toUtc);
            
            if (metrics.Count > 0)
            {
                metricsByChannel[channelId] = metrics;
                var channelMax = metrics.Max(m => m.Value);
                if (channelMax > maxValue)
                {
                    maxValue = channelMax;
                }
            }

            var events = dataSink.GetEventsForChannelInRange(channelId, fromUtc, toUtc);
            if (events.Count > 0)
            {
                eventsByChannel[channelId] = events;
            }
        }

        if (metricsByChannel.Count == 0)
        {
            DrawEmptyState(canvas, info);
            return;
        }

        if (maxValue == 0)
        {
            maxValue = 1;
        }
        
        // Add a 10% buffer on top so lines never touch the top edge.
        var maxDisplayValue = Math.Max(1d, maxValue * 1.1d);

        var totalMilliseconds = (toUtc - fromUtc).TotalMilliseconds;
        if (totalMilliseconds <= 0)
        {
            totalMilliseconds = 1;
        }

        using var axisPaint = new SKPaint
        {
            Color = new SKColor(70, 70, 85),
            StrokeWidth = 2,
            IsStroke = true,
            IsAntialias = true
        };

        using var gridPaint = new SKPaint
        {
            Color = new SKColor(40, 40, 50),
            StrokeWidth = 1,
            IsStroke = true,
            IsAntialias = true
        };

        using var textPaint = new SKPaint
        {
            Color = new SKColor(210, 210, 224),
            TextSize = 14,
            IsAntialias = true
        };

        using var legendTextPaint = new SKPaint
        {
            Color = new SKColor(210, 210, 224),
            TextSize = 12,
            IsAntialias = true
        };

        using var eventLabelPaint = new SKPaint
        {
            Color = new SKColor(230, 230, 230),
            TextSize = 12,
            IsAntialias = true
        };

        var gridLines = 4;
        var labelSamples = new List<string>(gridLines + 1);
        for (int i = 0; i <= gridLines; i++)
        {
            var valueRatio = 1f - (float)i / gridLines;
            var value = (long)Math.Ceiling(maxDisplayValue * valueRatio);
            labelSamples.Add(FormatBytes(value));
        }

        var maxLabelWidth = labelSamples.Count > 0
            ? labelSamples.Max(label => textPaint.MeasureText(label))
            : 0f;

        var legendChannels = metricsByChannel.Keys
            .Select(id => channelLookup.TryGetValue(id, out var channelInfo) ? channelInfo : null)
            .OfType<MaudeChannel>()
            .ToList();

        const float baseTopMargin = 24f;
        const float bottomMargin = 48f;
        const float rightMargin = 24f;
        const float legendSpacing = 18f;
        var legendHeight = legendChannels.Count * legendSpacing;
        var leftMargin = Math.Max(60f, maxLabelWidth + 22f);

        var chartRect = new SKRect(leftMargin,
                                   baseTopMargin + legendHeight + (legendHeight > 0 ? 8f : 0f),
                                   info.Width - rightMargin,
                                   info.Height - bottomMargin);

        if (chartRect.Width <= 0 || chartRect.Height <= 0)
        {
            return;
        }

        // Axes
        canvas.DrawLine(chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom, axisPaint);
        canvas.DrawLine(chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom, axisPaint);

        // Horizontal grid lines & labels
        for (int i = 0; i <= gridLines; i++)
        {
            var y = chartRect.Top + (chartRect.Height / gridLines) * i;
            canvas.DrawLine(chartRect.Left, y, chartRect.Right, y, gridPaint);

            var label = labelSamples[i];
            canvas.DrawText(label, chartRect.Left - 10 - textPaint.MeasureText(label), y + (textPaint.TextSize / 3), textPaint);
        }

        // Time labels
        var startLabel = fromUtc.ToLocalTime().ToString("HH:mm:ss");
        var endLabel = toUtc.ToLocalTime().ToString("HH:mm:ss");
        var midLabel = fromUtc.AddMilliseconds(totalMilliseconds / 2).ToLocalTime().ToString("HH:mm:ss");
        canvas.DrawText(startLabel, chartRect.Left, chartRect.Bottom + textPaint.TextSize + 4, textPaint);
        canvas.DrawText(midLabel, chartRect.MidX - textPaint.MeasureText(midLabel) / 2, chartRect.Bottom + textPaint.TextSize + 4, textPaint);
        canvas.DrawText(endLabel, chartRect.Right - textPaint.MeasureText(endLabel), chartRect.Bottom + textPaint.TextSize + 4, textPaint);

        // Legend
        var legendY = baseTopMargin + legendTextPaint.TextSize;
        foreach (var channelInfo in legendChannels)
        {
            var channelColor = ToSkColor(channelInfo.Color);
            using var legendPaint = new SKPaint
            {
                Color = channelColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            canvas.DrawCircle(chartRect.Left + 6, legendY - (legendTextPaint.TextSize / 3), 4, legendPaint);
            canvas.DrawText(channelInfo.Name, chartRect.Left + 14, legendY, legendTextPaint);
            legendY += legendSpacing;
        }

        // Lines
        foreach (var kv in metricsByChannel)
        {
            var channel = channelLookup.TryGetValue(kv.Key, out var channelInfo)
                ? channelInfo
                : new MaudeChannel(kv.Key, $"Channel {kv.Key}", Colors.Purple);
            var channelColor = ToSkColor(channel.Color);

            using var linePaint = new SKPaint
            {
                Color = channelColor,
                StrokeWidth = 3,
                IsStroke = true,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            using var pointPaint = new SKPaint
            {
                Color = channelColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            var path = new SKPath();
            var firstPoint = true;
            foreach (var metric in kv.Value)
            {
                var x = chartRect.Left + (float)((metric.CapturedAtUtc - fromUtc).TotalMilliseconds / totalMilliseconds) * chartRect.Width;
                var y = chartRect.Bottom - (float)(metric.Value / maxDisplayValue) * chartRect.Height;

                if (firstPoint)
                {
                    path.MoveTo(x, y);
                    firstPoint = false;
                }
                else
                {
                    path.LineTo(x, y);
                }

                canvas.DrawCircle(x, y, 2, pointPaint);
            }

            canvas.DrawPath(path, linePaint);
        }

        // Events
        foreach (var kv in eventsByChannel)
        {
            if (!metricsByChannel.TryGetValue(kv.Key, out var channelMetrics) || channelMetrics.Count == 0)
            {
                continue;
            }

            var channel = channelLookup.TryGetValue(kv.Key, out var channelInfo)
                ? channelInfo
                : new MaudeChannel(kv.Key, $"Channel {kv.Key}", Colors.Purple);

            var channelColor = ToSkColor(channel.Color);
            using var eventFill = new SKPaint
            {
                Color = channelColor.WithAlpha(180),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var eventStroke = new SKPaint
            {
                Color = SKColors.White,
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            foreach (var maudeEvent in kv.Value)
            {
                var metricForEvent = channelMetrics.LastOrDefault(m => m.CapturedAtUtc <= maudeEvent.CapturedAtUtc) ?? channelMetrics[^1];

                var x = chartRect.Left + (float)((maudeEvent.CapturedAtUtc - fromUtc).TotalMilliseconds / totalMilliseconds) * chartRect.Width;
                var y = chartRect.Bottom - (float)(metricForEvent.Value / maxDisplayValue) * chartRect.Height;

                canvas.DrawCircle(x, y, 6, eventFill);
                canvas.DrawCircle(x, y, 6, eventStroke);

                var labelOffset = eventLabelPaint.TextSize + 4;
                canvas.DrawText(maudeEvent.Label,
                                x + 8,
                                y - labelOffset,
                                eventLabelPaint);
            }
        }

        // Current position marker
        if (renderOptions.CurrentUtc.HasValue
            && renderOptions.CurrentUtc.Value >= fromUtc
            && renderOptions.CurrentUtc.Value <= toUtc)
        {
            var x = chartRect.Left + (float)((renderOptions.CurrentUtc.Value - fromUtc).TotalMilliseconds / totalMilliseconds) * chartRect.Width;
            using var nowPaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, 120),
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                PathEffect = SKPathEffect.CreateDash(new float[] { 6, 6 }, 0),
                IsAntialias = true
            };

            canvas.DrawLine(x, chartRect.Top, x, chartRect.Bottom, nowPaint);
        }
    }

    private static void DrawEmptyState(SKCanvas canvas, SKImageInfo info)
    {
        using var paint = new SKPaint
        {
            Color = new SKColor(150, 150, 160),
            TextSize = 16,
            IsAntialias = true
        };

        var message = "Waiting for samples...";
        var bounds = new SKRect();
        paint.MeasureText(message, ref bounds);
        var x = (info.Width - bounds.Width) / 2;
        var y = (info.Height + bounds.Height) / 2;
        canvas.DrawText(message, x, y, paint);
    }

    private static SKColor ToSkColor(Color color)
    {
        if (color == default)
        {
            return new SKColor(123, 97, 255);
        }

        return new SKColor((byte)(color.Red * 255),
                           (byte)(color.Green * 255),
                           (byte)(color.Blue * 255),
                           (byte)(color.Alpha * 255));
    }

    internal static string FormatBytes(long value)
    {
        double bytes = value;
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int unit = 0;
        while (bytes >= 1024 && unit < units.Length - 1)
        {
            bytes /= 1024;
            unit++;
        }

        return $"{bytes:0.#} {units[unit]}";
    }
}
