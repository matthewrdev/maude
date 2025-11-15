using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using SkiaSharp;

namespace Maude;

public static class MaudeChartRenderer
{
    private static readonly ThreadLocal<RenderResources> ThreadResources = new(() => new RenderResources());

    public static MaudeRenderResult Render(SKCanvas canvas,
                                           SKImageInfo info, 
                                           IMaudeDataSink dataSink,
                                           MaudeRenderOptions renderOptions)
    {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (dataSink == null) throw new ArgumentNullException(nameof(dataSink));
        if (renderOptions.Channels == null) throw new ArgumentNullException(nameof(renderOptions.Channels));

        var resources = ThreadResources.Value!;

        canvas.Clear(SKColors.Transparent);

        var bounds = new SKRect(0, 0, info.Width, info.Height);
        var backgroundColor = renderOptions.Mode == MaudeChartRenderMode.Overlay
            ? new SKColor(18, 18, 26, 190)
            : new SKColor(18, 18, 26);

        var surfaceBackground = resources.SurfaceBackgroundPaint;
        surfaceBackground.Color = backgroundColor;
        canvas.DrawRect(bounds, surfaceBackground);

        var fromUtc = renderOptions.FromUtc;
        var toUtc = renderOptions.ToUtc <= fromUtc
            ? fromUtc.AddMilliseconds(1)
            : renderOptions.ToUtc;

        var visibleChannels = (renderOptions.Channels.Count > 0
            ? renderOptions.Channels
            : dataSink.Channels.Select(c => c.Id)).ToArray();

        var channelLookup = resources.ChannelLookup;
        channelLookup.Clear();
        if (dataSink.Channels != null)
        {
            foreach (var channel in dataSink.Channels)
            {
                channelLookup[channel.Id] = channel;
            }
        }

        var channelSpans = resources.ChannelSpans;
        channelSpans.Clear();
        var channelSpanLookup = resources.ChannelSpanLookup;
        channelSpanLookup.Clear();
        double maxValue = 0;
        var hasMetrics = false;

        foreach (var channelId in visibleChannels)
        {
            var span = dataSink.GetMetricsChannelSpanForRange(channelId, fromUtc, toUtc);
            channelSpans.Add(span);
            channelSpanLookup[channelId] = span;

            if (span.Valid && span.Count > 0)
            {
                hasMetrics = true;
                if (span.MaxValue > maxValue)
                {
                    maxValue = span.MaxValue;
                }
            }
        }

        if (!hasMetrics)
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

        var axisPaint = resources.AxisPaint;
        var gridPaint = resources.GridPaint;

        var layoutScale = renderOptions.Mode == MaudeChartRenderMode.Overlay ? 0.65f : 1f;
        var textPaint = resources.TextPaint;
        textPaint.TextSize = 14 * layoutScale;

        var legendTextPaint = resources.LegendTextPaint;
        legendTextPaint.TextSize = 12 * layoutScale;

        var eventLabelPaint = resources.EventLabelPaint;
        eventLabelPaint.TextSize = 10 * layoutScale;

        var eventIconPaint = resources.EventIconPaint;
        eventIconPaint.TextSize = 18 * layoutScale;

        var eventLinePaint = resources.EventLinePaint;
        eventLinePaint.StrokeWidth = 1f * layoutScale;

        var eventVisuals = resources.EventVisuals;
        eventVisuals.Clear();

        var gridLines = 4;
        var labelSamples = resources.LabelSamples;
        labelSamples.Clear();
        for (int i = 0; i <= gridLines; i++)
        {
            var valueRatio = 1f - (float)i / gridLines;
            var value = (long)Math.Ceiling(maxDisplayValue * valueRatio);
            labelSamples.Add(FormatBytes(value));
        }

        var maxLabelWidth = labelSamples.Count > 0
            ? labelSamples.Max(label => textPaint.MeasureText(label))
            : 0f;

        var legendChannels = resources.LegendChannels;
        legendChannels.Clear();
        foreach (var span in channelSpans)
        {
            if (span.Valid
                && span.Count > 0
                && channelLookup.TryGetValue(span.ChannelId, out var channelInfo)
                && channelInfo != null)
            {
                legendChannels.Add(channelInfo);
            }
        }

        var baseTopMargin = 24f * layoutScale;
        var bottomMargin = 48f * layoutScale;
        var rightMargin = 24f * layoutScale;
        var leftMargin = Math.Max(60f * layoutScale, maxLabelWidth + 22f * layoutScale);

        var legendEntries = resources.LegendEntries;
        legendEntries.Clear();
        foreach (var channel in legendChannels)
        {
            legendEntries.Add(new LegendEntry(channel, legendTextPaint.MeasureText(channel.Name)));
        }

        var legendLineHeight = legendTextPaint.TextSize + 8f * layoutScale;
        var legendEntrySpacing = 18f * layoutScale;
        var availableLegendWidth = Math.Max(20f, info.Width - rightMargin - leftMargin);

        var legendLineStarts = resources.LegendLineStarts;
        legendLineStarts.Clear();

        if (legendEntries.Count > 0)
        {
            legendLineStarts.Add(0);
            float currentLineWidth = 0;

            for (var i = 0; i < legendEntries.Count; i++)
            {
                var entry = legendEntries[i];
                entry.TotalWidth = (8f * layoutScale * 2) + entry.TextWidth + legendEntrySpacing;

                if (currentLineWidth > 0 && currentLineWidth + entry.TotalWidth > availableLegendWidth)
                {
                    legendLineStarts.Add(i);
                    currentLineWidth = 0;
                }

                currentLineWidth += entry.TotalWidth;
                legendEntries[i] = entry;
            }

            legendLineStarts.Add(legendEntries.Count);
        }

        var legendLineCount = legendLineStarts.Count > 0 ? legendLineStarts.Count - 1 : 0;
        var legendHeight = legendLineCount * legendLineHeight;

        var chartRect = new SKRect(leftMargin,
                                   baseTopMargin + legendHeight + (legendHeight > 0 ? 8f * layoutScale : 0f),
                                   info.Width - rightMargin,
                                   info.Height - bottomMargin);

        if (chartRect.Width <= 0 || chartRect.Height <= 0)
        {
            return MaudeRenderResult.Empty;
        }

        // Prepare event visuals so backing lines can render behind the chart data.
        foreach (var channelId in visibleChannels)
        {
            var channel = channelLookup.TryGetValue(channelId, out var channelInfo)
                ? channelInfo
                : new MaudeChannel(channelId, $"Channel {channelId}", Colors.Purple);

            var channelColor = ToSkColor(channel.Color);
            var isDetached = channelId == MaudeConstants.ReservedChannels.ChannelNotSpecified_Id;
            var span = channelSpanLookup.TryGetValue(channelId, out var lookupSpan) ? lookupSpan : default;
            var hasMetricsForChannel = span.Valid;

            dataSink.UseEventsInChannelForRange(channelId, fromUtc, toUtc, eventSpan =>
            {
                if (eventSpan.IsEmpty)
                {
                    return;
                }

                var useMetrics = hasMetricsForChannel && !isDetached;

                foreach (var maudeEvent in eventSpan)
                {
                    var icon = string.IsNullOrWhiteSpace(maudeEvent.Icon)
                        ? MaudeConstants.DefaultEventIcon
                        : maudeEvent.Icon;

                    var x = chartRect.Left + (float)((maudeEvent.CapturedAtUtc - fromUtc).TotalMilliseconds / totalMilliseconds) * chartRect.Width;
                    float y;

                    if (useMetrics)
                    {
                        long? metricValue = null;
                        dataSink.UseMetricsInChannelForRange(channelId, fromUtc, toUtc, metricsSpan =>
                        {
                            metricValue = GetMetricValueAt(metricsSpan, maudeEvent.CapturedAtUtc);
                        });

                        y = metricValue.HasValue
                            ? chartRect.Bottom - (float)(metricValue.Value / maxDisplayValue) * chartRect.Height
                            : chartRect.Bottom - (8f * layoutScale);
                    }
                    else
                    {
                        y = chartRect.Bottom - (8f * layoutScale);
                    }

                    eventVisuals.Add(new EventVisual
                    {
                        X = x,
                        Y = y,
                        Icon = icon,
                        Label = maudeEvent.Label,
                        Color = channelColor
                    });
                }
            });
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
        for (var lineIndex = 0; lineIndex < legendLineCount; lineIndex++)
        {
            var startIndex = legendLineStarts[lineIndex];
            var endIndex = legendLineStarts[lineIndex + 1];
            var legendX = leftMargin;

            for (var entryIndex = startIndex; entryIndex < endIndex; entryIndex++)
            {
                var entry = legendEntries[entryIndex];
                var channelColor = ToSkColor(entry.Channel.Color);
                var iconRadius = 4f * layoutScale;
                var iconCenterY = legendY - (legendTextPaint.TextSize / 3);

                var legendPaint = resources.LegendPaint;
                legendPaint.Color = channelColor;
                canvas.DrawCircle(legendX + iconRadius, iconCenterY, iconRadius, legendPaint);
                canvas.DrawText(entry.Channel.Name, legendX + iconRadius * 2 + 4f * layoutScale, legendY, legendTextPaint);
                legendX += entry.TotalWidth;
            }

            legendY += legendLineHeight;
        }

        foreach (var visual in eventVisuals)
        {
            eventLinePaint.Color = visual.Color.WithAlpha(120);
            canvas.DrawLine(visual.X, chartRect.Top, visual.X, chartRect.Bottom, eventLinePaint);
        }

        var linePaint = resources.LinePaint;
        linePaint.StrokeWidth = 2f * layoutScale;
        var pointPaint = resources.PointPaint;
        var linePath = resources.LinePath;
        var pointRadius = 2f * layoutScale;

        // Lines
        foreach (var span in channelSpans)
        {
            if (!span.Valid)
            {
                continue;
            }

            var channelId = span.ChannelId;
            var channel = channelLookup.TryGetValue(channelId, out var channelInfo)
                ? channelInfo
                : new MaudeChannel(channelId, $"Channel {channelId}", Colors.Purple);
            var channelColor = ToSkColor(channel.Color);

            linePaint.Color = channelColor;
            pointPaint.Color = channelColor;

            dataSink.UseMetricsInChannelForRange(channelId, fromUtc, toUtc, metricsSpan =>
            {
                if (metricsSpan.IsEmpty)
                {
                    return;
                }

                linePath.Reset();
                var firstPoint = true;
                foreach (var metric in metricsSpan)
                {
                    var x = chartRect.Left + (float)((metric.CapturedAtUtc - fromUtc).TotalMilliseconds / totalMilliseconds) * chartRect.Width;
                    var y = chartRect.Bottom - (float)(metric.Value / maxDisplayValue) * chartRect.Height;

                    if (firstPoint)
                    {
                        linePath.MoveTo(x, y);
                        firstPoint = false;
                    }
                    else
                    {
                        linePath.LineTo(x, y);
                    }

                    canvas.DrawCircle(x, y, pointRadius, pointPaint);
                }

                canvas.DrawPath(linePath, linePaint);
            });
        }

        // Events
        var iconMetrics = eventIconPaint.FontMetrics;
        foreach (var visual in eventVisuals)
        {
            var iconBaselineY = visual.Y - (iconMetrics.Ascent + iconMetrics.Descent) / 2f;
            canvas.DrawText(visual.Icon, visual.X, iconBaselineY, eventIconPaint);

            var labelOffset = eventLabelPaint.TextSize + eventIconPaint.TextSize * 0.25f + 4 * layoutScale;
            var labelX = visual.X - (eventLabelPaint.MeasureText(visual.Label) / 2f);
            canvas.DrawText(visual.Label,
                            labelX,
                            visual.Y - labelOffset,
                            eventLabelPaint);
        }

        // Current position marker or probe marker
        if (markerUtc.HasValue
            && markerUtc.Value >= fromUtc
            && markerUtc.Value <= toUtc)
        {
            var x = chartRect.Left + (float)((markerUtc.Value - fromUtc).TotalMilliseconds / totalMilliseconds) * chartRect.Width;
            var markerPaint = resources.MarkerPaint;
            canvas.DrawLine(x, chartRect.Top, x, chartRect.Bottom, markerPaint);
        }

        if (probeUtc.HasValue)
        {
            var highlightLines = resources.HighlightLines;
            highlightLines.Clear();
            foreach (var channelInfo in legendChannels)
            {
                if (!channelSpanLookup.TryGetValue(channelInfo.Id, out var channelSpan) || !channelSpan.Valid)
                {
                    continue;
                }

                long? value = null;
                dataSink.UseMetricsInChannelForRange(channelInfo.Id, fromUtc, toUtc, metricsSpan =>
                {
                    value = GetMetricValueAt(metricsSpan, probeUtc.Value);
                });
                if (value.HasValue)
                {
                    highlightLines.Add(($"{channelInfo.Name}: {FormatBytes(value.Value)}", ToSkColor(channelInfo.Color)));
                }
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

                var panelFill = resources.HighlightPanelPaint;
                panelFill.Color = new SKColor(12, 12, 18, 200);
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

    private static long? GetMetricValueAt(ReadOnlySpan<MaudeMetric> metrics, DateTime targetUtc)
    {
        if (metrics.IsEmpty)
        {
            return null;
        }

        for (var i = metrics.Length - 1; i >= 0; i--)
        {
            var metric = metrics[i];
            if (metric.CapturedAtUtc <= targetUtc)
            {
                return metric.Value;
            }
        }

        return metrics[0].Value;
    }

    private static SKTypeface LoadMaterialSymbolsTypeface()
    {
        return LoadFontFromResources("MaterialSymbolsOutlined.ttf");
    }

    private static SKTypeface LoadFontFromResources(string fontResourceName)
    {
        using (var stream = FileSystem.OpenAppPackageFileAsync(fontResourceName).Result)
        {
            if (stream == null)
            {
                throw new FileNotFoundException($"Font resource '{fontResourceName}' not found.");
            }

            return SKTypeface.FromStream(stream);
        }
    }

    private sealed class RenderResources
    {
        public RenderResources()
        {
            SurfaceBackgroundPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill
            };

            AxisPaint = new SKPaint
            {
                Color = new SKColor(70, 70, 85),
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsStroke = true,
                IsAntialias = true
            };

            GridPaint = new SKPaint
            {
                Color = new SKColor(40, 40, 50),
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke,
                IsStroke = true,
                IsAntialias = true
            };

            TextPaint = new SKPaint
            {
                Color = new SKColor(210, 210, 224),
                IsAntialias = true
            };

            LegendTextPaint = new SKPaint
            {
                Color = new SKColor(210, 210, 224),
                IsAntialias = true
            };

            EventLabelPaint = new SKPaint
            {
                Color = new SKColor(230, 230, 230),
                IsAntialias = true
            };

            MaterialSymbolsTypeface = LoadMaterialSymbolsTypeface();

            EventIconPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                Typeface = MaterialSymbolsTypeface,
                TextEncoding = SKTextEncoding.Utf16
            };

            EventLinePaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, 40),
                StrokeWidth = 1,
                Style = SKPaintStyle.Stroke,
                IsStroke = true,
                IsAntialias = true
            };

            LegendPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            LinePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                IsStroke = true,
                IsAntialias = true
            };

            PointPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            HighlightPanelPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            MarkerPathEffect = SKPathEffect.CreateDash(new float[] { 6, 6 }, 0);
            MarkerPaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, 140),
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                PathEffect = MarkerPathEffect
            };

            LinePath = new SKPath();
        }

        public SKPaint SurfaceBackgroundPaint { get; }
        public SKPaint AxisPaint { get; }
        public SKPaint GridPaint { get; }
        public SKPaint TextPaint { get; }
        public SKPaint LegendTextPaint { get; }
        public SKPaint EventLabelPaint { get; }
        public SKPaint EventIconPaint { get; }
        public SKPaint EventLinePaint { get; }
        public SKTypeface MaterialSymbolsTypeface { get; }
        public SKPaint LegendPaint { get; }
        public SKPaint LinePaint { get; }
        public SKPaint PointPaint { get; }
        public SKPaint HighlightPanelPaint { get; }
        public SKPaint MarkerPaint { get; }
        public SKPathEffect MarkerPathEffect { get; }
        public SKPath LinePath { get; }

        public Dictionary<byte, MaudeChannel> ChannelLookup { get; } = new();
        public Dictionary<byte, MaudeChannelSpan> ChannelSpanLookup { get; } = new();
        public List<MaudeChannelSpan> ChannelSpans { get; } = new();
        public List<string> LabelSamples { get; } = new();
        public List<MaudeChannel> LegendChannels { get; } = new();
        public List<LegendEntry> LegendEntries { get; } = new();
        public List<int> LegendLineStarts { get; } = new();
        public List<(string Text, SKColor Color)> HighlightLines { get; } = new();
        public List<EventVisual> EventVisuals { get; } = new();
    }

    private struct LegendEntry
    {
        public LegendEntry(MaudeChannel channel, float textWidth)
        {
            Channel = channel;
            TextWidth = textWidth;
            TotalWidth = 0f;
        }

        public MaudeChannel Channel { get; }

        public float TextWidth { get; }

        public float TotalWidth { get; set; }
    }

    private struct EventVisual
    {
        public float X { get; init; }
        public float Y { get; init; }
        public string Icon { get; init; }
        public string Label { get; init; }
        public SKColor Color { get; init; }
    }
}
