using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;
using SkiaSharp;

namespace Maude;

public static class MaudeChartRenderer
{
    public static MaudeRenderResult Render(SKCanvas canvas,
                                           SKImageInfo info, 
                                           IMaudeDataSink dataSink,
                                           MaudeRenderOptions renderOptions)
    {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (dataSink == null) throw new ArgumentNullException(nameof(dataSink));
        if (renderOptions.Channels == null) throw new ArgumentNullException(nameof(renderOptions.Channels));

        canvas.Clear(SKColors.Transparent);

        var bounds = new SKRect(0, 0, info.Width, info.Height);
        var backgroundColor = renderOptions.Mode == MaudeChartRenderMode.Overlay
            ? new SKColor(18, 18, 26, 190)
            : new SKColor(18, 18, 26);

        using var surfaceBackground = new SKPaint
        {
            Color = backgroundColor,
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
            return MaudeRenderResult.Empty;
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

        var probeRatio = renderOptions.ProbePosition.HasValue
            ? Math.Clamp(renderOptions.ProbePosition.Value, 0f, 1f)
            : (float?)null;

        DateTime? probeUtc = null;
        if (probeRatio.HasValue)
        {
            probeUtc = fromUtc.AddMilliseconds(totalMilliseconds * probeRatio.Value);
        }

        var markerUtc = probeUtc ?? renderOptions.CurrentUtc;

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

        var layoutScale = renderOptions.Mode == MaudeChartRenderMode.Overlay ? 0.65f : 1f;
        using var textPaint = new SKPaint
        {
            Color = new SKColor(210, 210, 224),
            TextSize = 14 * layoutScale,
            IsAntialias = true
        };

        using var legendTextPaint = new SKPaint
        {
            Color = new SKColor(210, 210, 224),
            TextSize = 12 * layoutScale,
            IsAntialias = true
        };

        using var eventLabelPaint = new SKPaint
        {
            Color = new SKColor(230, 230, 230),
            TextSize = 12 * layoutScale,
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

        var baseTopMargin = 24f * layoutScale;
        var bottomMargin = 48f * layoutScale;
        var rightMargin = 24f * layoutScale;
        var leftMargin = Math.Max(60f * layoutScale, maxLabelWidth + 22f * layoutScale);

        var legendEntries = legendChannels
            .Select(channel => new LegendEntry(channel, legendTextPaint.MeasureText(channel.Name)))
            .ToList();

        var legendLineHeight = legendTextPaint.TextSize + 8f * layoutScale;
        var legendEntrySpacing = 18f * layoutScale;
        var availableLegendWidth = Math.Max(20f, info.Width - rightMargin - leftMargin);

        var legendLines = new List<List<LegendEntry>>();
        var currentLine = new List<LegendEntry>();
        float currentLineWidth = 0;

        foreach (var entry in legendEntries)
        {
            entry.TotalWidth = (8f * layoutScale * 2) + entry.TextWidth + legendEntrySpacing;
            if (currentLineWidth > 0 && currentLineWidth + entry.TotalWidth > availableLegendWidth)
            {
                legendLines.Add(currentLine);
                currentLine = new List<LegendEntry>();
                currentLineWidth = 0;
            }

            currentLine.Add(entry);
            currentLineWidth += entry.TotalWidth;
        }

        if (currentLine.Count > 0)
        {
            legendLines.Add(currentLine);
        }

        var legendHeight = legendLines.Count * legendLineHeight;

        var chartRect = new SKRect(leftMargin,
                                   baseTopMargin + legendHeight + (legendHeight > 0 ? 8f * layoutScale : 0f),
                                   info.Width - rightMargin,
                                   info.Height - bottomMargin);

        if (chartRect.Width <= 0 || chartRect.Height <= 0)
        {
            return MaudeRenderResult.Empty;
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
        foreach (var line in legendLines)
        {
            var legendX = leftMargin;
            foreach (var entry in line)
            {
                var channelColor = ToSkColor(entry.Channel.Color);
                using var legendPaint = new SKPaint
                {
                    Color = channelColor,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                var iconRadius = 4f * layoutScale;
                var iconCenterY = legendY - (legendTextPaint.TextSize / 3);
                canvas.DrawCircle(legendX + iconRadius, iconCenterY, iconRadius, legendPaint);
                canvas.DrawText(entry.Channel.Name, legendX + iconRadius * 2 + 4f * layoutScale, legendY, legendTextPaint);
                legendX += entry.TotalWidth;
            }

            legendY += legendLineHeight;
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
                StrokeWidth = 2f * layoutScale,
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

                canvas.DrawCircle(x, y, 2f * layoutScale, pointPaint);
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

                var eventRadius = 6f * layoutScale;
                canvas.DrawCircle(x, y, eventRadius, eventFill);
                canvas.DrawCircle(x, y, eventRadius, eventStroke);

                var labelOffset = eventLabelPaint.TextSize + 4 * layoutScale;
                canvas.DrawText(maudeEvent.Label,
                                x + 8,
                                y - labelOffset,
                                eventLabelPaint);
            }
        }

        // Current position marker or probe marker
        if (markerUtc.HasValue
            && markerUtc.Value >= fromUtc
            && markerUtc.Value <= toUtc)
        {
            var x = chartRect.Left + (float)((markerUtc.Value - fromUtc).TotalMilliseconds / totalMilliseconds) * chartRect.Width;
            using var markerPaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, 140),
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                PathEffect = SKPathEffect.CreateDash(new float[] { 6, 6 }, 0),
                IsAntialias = true
            };

            canvas.DrawLine(x, chartRect.Top, x, chartRect.Bottom, markerPaint);
        }

        if (probeUtc.HasValue)
        {
            var highlightLines = new List<(string Text, SKColor Color)>();
            foreach (var channelInfo in legendChannels)
            {
                if (!metricsByChannel.TryGetValue(channelInfo.Id, out var channelMetrics))
                {
                    continue;
                }

                var value = GetMetricValueAt(channelMetrics, probeUtc.Value);
                if (!value.HasValue)
                {
                    continue;
                }

                highlightLines.Add(($"{channelInfo.Name}: {FormatBytes(value.Value)}", ToSkColor(channelInfo.Color)));
            }

            if (highlightLines.Count > 0)
            {
                var headerText = probeUtc.Value.ToLocalTime().ToString("HH:mm:ss");
                var lineHeight = textPaint.TextSize + 4f * layoutScale;
                var padding = 8f * layoutScale;
                var maxTextWidth = Math.Max(textPaint.MeasureText(headerText),
                    highlightLines.Max(l => textPaint.MeasureText(l.Text)));
                var panelWidth = maxTextWidth + padding * 2;
                var panelHeight = (highlightLines.Count + 1) * lineHeight + padding * 2;
                var panelRect = new SKRect(
                    Math.Max(chartRect.Left + 4f, chartRect.Right - panelWidth - 4f),
                    chartRect.Top + 4f,
                    chartRect.Right - 4f,
                    chartRect.Top + 4f + panelHeight);

                using var panelFill = new SKPaint
                {
                    Color = new SKColor(12, 12, 18, 200),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                canvas.DrawRoundRect(panelRect, 6f * layoutScale, 6f * layoutScale, panelFill);

                var originalColor = textPaint.Color;
                var textY = panelRect.Top + padding + textPaint.TextSize;
                textPaint.Color = new SKColor(255, 255, 255);
                canvas.DrawText(headerText, panelRect.Left + padding, textY, textPaint);
                textY += lineHeight;

                foreach (var line in highlightLines)
                {
                    textPaint.Color = line.Color;
                    canvas.DrawText(line.Text, panelRect.Left + padding, textY, textPaint);
                    textY += lineHeight;
                }

                textPaint.Color = originalColor;
            }
        }

        return new MaudeRenderResult(chartRect, true);
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

    private static long? GetMetricValueAt(IReadOnlyList<MaudeMetric> metrics, DateTime targetUtc)
    {
        if (metrics == null || metrics.Count == 0)
        {
            return null;
        }

        for (var i = metrics.Count - 1; i >= 0; i--)
        {
            var metric = metrics[i];
            if (metric.CapturedAtUtc <= targetUtc)
            {
                return metric.Value;
            }
        }

        return metrics[0].Value;
    }

    private sealed class LegendEntry
    {
        public LegendEntry(MaudeChannel channel, float textWidth)
        {
            Channel = channel;
            TextWidth = textWidth;
        }

        public MaudeChannel Channel { get; }

        public float TextWidth { get; }

        public float TotalWidth { get; set; }
    }
}
