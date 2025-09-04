using System.Text;
using Ansight.Adb.Telemetry;
using Ansight.Studio.Telemetry;
using Ansight.TimeZones;
using SkiaSharp;

namespace Maude.Runtime.Views.Telemetry
{
    public class ChartRenderer : IChartRenderer
    {
        protected const int margin = 16;
        protected const int spacing = 8;

        protected const int verticalAxisLabelsWidth = 60;

        protected readonly int topMargin = margin;
        protected readonly int bottomMargin = margin / 2;
        protected readonly int leftMargin = margin;
        protected readonly int rightMargin = margin;

        public ChartRenderer(IChartRenderingOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IChartRenderingOptions Options { get; }

        public void Dispose()
        {
            OnDisposed();
        }

        protected virtual void OnDisposed()
        {
        }

        public void Render(SKSurface surface,
                           int canvasWidth,
                           int canvasHeight,
                           DateTime playPosition,
                           bool shouldShowPosition,
                           Point? hoverPosition,
                           ITimeZone timeZone,
                           DateTime startTimeUtc,
                           TelemetryTimeDisplayMode timeDisplayMode,
                           string axisLabel,
                           string dateTimeFormatString,
                           int verticalAxisIntervalAmount,
                           ITelemetryChannel telemetryChannel,
                           IReadOnlyList<string> excludedGroups)
        {
            surface.Canvas.Clear(Options.BackgroundColor);
            DateTime segmentStartUtc, segmentEndUtc;

            CalculateDisplayAreaStartEndDateTimes(playPosition,
                                                  startTimeUtc,
                                                  timeDisplayMode,
                                                  out segmentStartUtc,
                                                  out segmentEndUtc);

            var segments = telemetryChannel.GetSegmentsInRange(segmentStartUtc, segmentEndUtc);

            var minValue = 0;
            var maxValue = CalculateMaxDisplayValue(segments);

            float chartStartX, chartEndX, chartStartY, chartEndY;

            CalculateChartBounds(canvasWidth,
                                canvasHeight,
                                Options.TextSize,
                                out chartStartX,
                                out chartEndX,
                                out chartStartY,
                                out chartEndY);

            hoverPosition = ConstrainToChartBounds(chartStartX,
                                                   chartStartY,
                                                   chartEndX,
                                                   chartEndY,
                                                   hoverPosition);

            RenderAxesLines(surface,
                            chartStartX,
                            chartStartY,
                            chartEndX,
                            chartEndY,
                            Options.AxesBarColor);

            RenderHorizontalAxisIntervals(surface,
                                          chartStartX,
                                          chartStartY,
                                          chartEndX,
                                          chartEndY,
                                          startTimeUtc,
                                          segmentStartUtc,
                                          segmentEndUtc,
                                          timeZone,
                                          dateTimeFormatString,
                                          timeDisplayMode,
                                          Options.IntervalBarColor,
                                          Options.IntervalTextColor,
                                          Options.TextSize);

            RenderVerticalAxisIntervals(surface,
                                        chartStartX,
                                        chartStartY,
                                        chartEndX,
                                        chartEndY,
                                        minValue,
                                        maxValue,
                                        axisLabel,
                                        Options.IntervalBarColor,
                                        Options.IntervalTextColor,
                                        Options.TextSize);

            RenderHoverPosition(surface,
                                chartStartX,
                                chartStartY,
                                chartEndX,
                                chartEndY,
                                hoverPosition,
                                Options.HoverBarColor);

            RenderLineContent(surface,
                              chartStartX,
                              chartStartY,
                              chartEndX,
                              chartEndY,
                              segmentStartUtc,
                              segmentEndUtc,
                              timeZone,
                              timeDisplayMode,
                              minValue,
                              (float)maxValue,
                              hoverPosition,
                              axisLabel,
                              telemetryChannel,
                              excludedGroups,
                              Options);

            if (shouldShowPosition)
            {
                RenderPosition(surface,
                               chartStartX,
                               chartStartY,
                               chartEndX,
                               chartEndY,
                               playPosition,
                               startTimeUtc,
                               segmentStartUtc,
                               segmentEndUtc,
                               timeZone,
                               dateTimeFormatString,
                               timeDisplayMode,
                               Options.PositionBarColor,
                               Options.PositionTextColor,
                               Options.TextSize);
            }
        }

        private void CalculateChartBounds(int width,
                                          int height,
                                          float textSize,
                                          out float chartStartX,
                                          out float chartEndX,
                                          out float chartStartY,
                                          out float chartEndY)
        {

            var horizontalAxisLabelsPositionX = leftMargin;
            var horizontalAxisLabelsPositionY = height - bottomMargin - textSize;

            chartStartX = horizontalAxisLabelsPositionX + verticalAxisLabelsWidth + spacing;
            chartEndX = width - margin;
            chartStartY = topMargin;
            chartEndY = horizontalAxisLabelsPositionY - spacing;
        }

        private void RenderHoverPosition(SKSurface surface,
                                         float chartStartX,
                                         float chartStartY,
                                         float chartEndX,
                                         float chartEndY,
                                         Point? hoverPosition,
                                         SKColor hoverBarColor)
        {
            if (hoverPosition.HasValue == false)
            {
                return;
            }


            var linePaint = new SKPaint()
            {
                IsStroke = true,
                Color = hoverBarColor,
                StrokeWidth = 2,
                PathEffect = SKPathEffect.CreateDash(new float[] { 10, 5 }, 5),
            };

            try
            {
                surface.Canvas.DrawLine((float)hoverPosition.Value.X, chartStartY, (float)hoverPosition.Value.X, chartEndY, linePaint);
            }
            finally
            {
                linePaint.Dispose();
            }
        }

        private Point? ConstrainToChartBounds(float chartStartX,
                                              float chartStartY,
                                              float chartEndX,
                                              float chartEndY,
                                              Point? position)
        {
            if (!position.HasValue)
            {
                return null;
            }

            // Is the outside of the chart start?
            if (position.Value.X < chartStartX
                || position.Value.Y < chartStartY)
            {
                return null;
            }

            // Is the position outside of the chart end?
            if (position.Value.X > chartEndX
                || position.Value.Y > chartEndY)
            {
                return null;
            }

            // Position is within bounds
            return position;
        }

        public void CalculateDisplayAreaStartEndDateTimes(DateTime position,
                                                          DateTime startTimeUtc,
                                                          TelemetryTimeDisplayMode timeDisplayMode,
                                                          out DateTime segmentStartUtc,
                                                          out DateTime segmentEndUtc)
        {
            segmentStartUtc = new DateTime(position.Year,
                                           position.Month,
                                           position.Day,
                                           position.Hour,
                                           position.Minute,
                                           0,
                                           DateTimeKind.Utc);

            if (timeDisplayMode == TelemetryTimeDisplayMode.Relative)
            {
                var positionOffsetMinutes = Math.Floor((position - startTimeUtc).TotalMinutes);
                if (positionOffsetMinutes < 0)
                {
                    positionOffsetMinutes = 0;
                }
                segmentStartUtc = startTimeUtc + TimeSpan.FromMinutes(positionOffsetMinutes);
            }
            segmentEndUtc = segmentStartUtc + TimeSpan.FromMinutes(1); // TODO: The segment range should be configurable
        }

        private void RenderLineContent(SKSurface surface,
                                       float chartStartX,
                                       float chartStartY,
                                       float chartEndX,
                                       float chartEndY,
                                       DateTime segmentStartUtc,
                                       DateTime segmentEndUtc,
                                       ITimeZone timeZone,
                                       TelemetryTimeDisplayMode timeDisplayMode,
                                       float minValue,
                                       float maxValue,
                                       Point? hoverPosition,
                                       string axisLabel,
                                       ITelemetryChannel telemetryChannel,
                                       IReadOnlyList<string> excludedGroups,
                                       IChartRenderingOptions options)
        {
            const float axisCorrectionOffset = 2;

            float horizontalRangeMilliseconds = (float)(segmentEndUtc - segmentStartUtc).TotalMilliseconds;
            float verticalRange = maxValue - minValue;

            float xPositionRange = (chartEndX - chartStartX) - axisCorrectionOffset;
            float yPositionRange = (chartEndY - chartStartY) - axisCorrectionOffset;
            foreach (var group in telemetryChannel.Groups)
            {
                // Validate that the group should be rendered.
                if (excludedGroups != null && excludedGroups.Contains(group))
                {
                    continue;
                }

                var segments = telemetryChannel.GetSegmentsInRangeForGroup(group, segmentStartUtc, segmentEndUtc);
                if (segments.Count == 0)
                {
                    continue;
                }

                var color = options.GetGroupColor(group);

                var linePaint = new SKPaint()
                {
                    IsStroke = true,
                    Color = color,
                    StrokeWidth = 2,
                    IsAntialias = true,
                };

                var textPaint = new SKPaint()
                {
                    IsStroke = true,
                    Color = color,
                    IsAntialias = true,
                    TextSize = options.ValueTextSize,
                };

                try
                {
                    TelemetryDataPoint? hoverStartData = null, hoverEndData = null;
                    SKPoint? hoverStartPoint = null, hoverEndPoint = null;

                    foreach (var segment in segments)
                    {
                        var points = segment.GetPointsInRange(segmentStartUtc, segmentEndUtc);
                        if (points.Count == 0)
                        {
                            continue;
                        }

                        var skPoints = new SKPoint[points.Count];
                        for (var i = 0; i < points.Count; ++i)
                        {
                            var dataPoint = points[i];
                            var timeOffset = (float)(points[i].DateTimeUtc - segmentStartUtc).TotalMilliseconds;
                            float timeOffsetScale = timeOffset / horizontalRangeMilliseconds;
                            float valueOffsetScale = 1 - (float)dataPoint.Value / verticalRange;
                            float xPosition = chartStartX + (xPositionRange * timeOffsetScale) + axisCorrectionOffset;
                            float yPosition = chartStartY + (yPositionRange * valueOffsetScale) + axisCorrectionOffset;

                            var point = new SKPoint(xPosition, yPosition);
                            if (i < skPoints.Length)
                            {
                                skPoints[i] = point;
                            }

                            // Try to calculate the hover point.
                            if (hoverPosition.HasValue && hoverEndData == null)
                            {
                                if (point.X <= hoverPosition.Value.X)
                                {
                                    hoverStartPoint = point;
                                    hoverStartData = dataPoint;
                                }

                                if (point.X >= hoverPosition.Value.X)
                                {
                                    hoverEndPoint = point;
                                    hoverEndData = dataPoint;
                                }
                            }
                        }

                        surface.Canvas.DrawPoints(SKPointMode.Polygon, skPoints, linePaint);

                        // Render the hover 
                        if (hoverStartData != null
                            && hoverEndData != null
                            && hoverStartPoint != null
                            && hoverEndPoint != null)
                        {
                            var intepolationRate = (hoverPosition.Value.X - hoverStartPoint.Value.X) / (hoverEndPoint.Value.X - hoverStartPoint.Value.X);
                            var pointX = hoverStartPoint.Value.X + ((hoverEndPoint.Value.X - hoverStartPoint.Value.X) * intepolationRate);
                            var pointY = hoverStartPoint.Value.Y + ((hoverEndPoint.Value.Y - hoverStartPoint.Value.Y) * intepolationRate);

                            var value = hoverStartData.Value.Value + ((hoverEndData.Value.Value - hoverStartData.Value.Value) * intepolationRate);

                            surface.Canvas.DrawCircle((float)pointX, (float)pointY, 3.0f, linePaint);

                            var valueString = GetLabelledValue(value, axisLabel).Replace(Environment.NewLine, " ");

                            var textBounds = new SKRect();
                            textPaint.MeasureText(valueString, ref textBounds);

                            surface.Canvas.DrawText(valueString, (float)(pointX - textBounds.Width / 2), (float)(pointY - (textBounds.Height) + 4), textPaint);
                        }

                        hoverStartData = hoverEndData = null;
                        hoverStartPoint = hoverEndPoint = null;
                    }
                }
                finally
                {
                    linePaint.Dispose();
                    textPaint.Dispose();
                }
            }    
        }

        protected virtual string GetLabelledValue(double value, string axisSuffix)
        {
            return $"{value} ({axisSuffix})";
        }

        private void RenderPosition(SKSurface surface,
                                    float chartStartX,
                                    float chartStartY,
                                    float chartEndX,
                                    float chartEndY,
                                    DateTime position,
                                    DateTime startUtc,
                                    DateTime segmentStartUtc,
                                    DateTime segmentEndUtc,
                                    ITimeZone timeZone,
                                    string dateTimeFormatString,
                                    TelemetryTimeDisplayMode timeDisplayMode,
                                    SKColor positionBarColor,
                                    SKColor positionTextColor,
                                    float textSize)
        {
            float positionPercent = (float)((position - segmentStartUtc).TotalMilliseconds / (segmentEndUtc - segmentStartUtc).TotalMilliseconds);

            var positionX = chartStartX + ((chartEndX - chartStartX) * positionPercent);

            var linePaint = new SKPaint()
            {
                IsStroke = true,
                Color = positionBarColor,
                StrokeWidth = 2,
            };

            try
            {
                surface.Canvas.DrawLine(positionX, chartStartY, positionX, chartEndY, linePaint);
            }
            finally
            {
                linePaint.Dispose();
            }

            var textPaint = new SKPaint()
            {
                Color = positionTextColor,
                TextSize = textSize,
            };

            var text = (position + timeZone.Offset).ToString(dateTimeFormatString);

            if (timeDisplayMode == TelemetryTimeDisplayMode.Relative)
            {
                var offsetTimeSpan = (position - startUtc);

                if (offsetTimeSpan.TotalMilliseconds < 0)
                {
                    offsetTimeSpan = new TimeSpan(0);
                }

                text = new DateTime(offsetTimeSpan.Ticks).ToString(dateTimeFormatString);
            }

            var textBounds = new SKRect();
            textPaint.MeasureText(text, ref textBounds);

            try
            {
                surface.Canvas.DrawText(text, positionX - textBounds.Width / 2, chartStartY + (textBounds.Height / 2), textPaint);
            }
            finally
            {
                textPaint.Dispose();
            }
        }

        private void RenderVerticalAxisIntervals(SKSurface surface,
                                                 float chartStartX,
                                                 float chartStartY,
                                                 float chartEndX,
                                                 float chartEndY,
                                                 int minValue,
                                                 double maxValue,
                                                 string axisLabel,
                                                 SKColor intervalBarColor,
                                                 SKColor intervalTextColor,
                                                 float textSize)
        {
            var textPaint = new SKPaint()
            {
                Color = intervalTextColor,
                TextSize = textSize,
            };

            var linePaint = new SKPaint()
            {
                IsStroke = true,
                Color = intervalBarColor,
                StrokeWidth = 2,
                PathEffect = SKPathEffect.CreateDash(new float[] { 20, 10 }, 20),
            };

            try
            {
                bool isFirst = true;
                // Draw first interval (bottom axis bar)
                for (var interval = 0.0; interval < 1.1d; interval += 0.5d)
                {
                    var positionY = (chartEndY) - (float)((chartEndY - chartStartY) * interval);
                    if (!isFirst)
                    {
                        surface.Canvas.DrawLine(chartStartX + 1, positionY, chartEndX, positionY, linePaint);
                    }

                    var value = (maxValue - minValue) * interval;
                    var text = GetLabelledValue(value, axisLabel);
                    var textBounds = new SKRect();
                    textPaint.MeasureText(text, ref textBounds);

                    var lineHeight = textPaint.TextSize * 1.05f;
                    var lines = SplitLines(text, textPaint);


                    var y = (float)(positionY) - ((float)lines.Length * lineHeight / 2.0f);
                    if (interval >= 1.0d) // Handle top interval and ensure its on-screen.
                    {
                        y = positionY;
                    }

                    foreach (var line in lines)
                    {
                        y += lineHeight / 2.0f;
                        var x = margin / 2;
                        surface.Canvas.DrawText(line.Value, x, y, textPaint);
                        y += lineHeight / 2.0f;
                    }

                    isFirst = false;
                }
            }
            finally
            {
                textPaint.Dispose();
                linePaint.Dispose();
            }
        }

        public class Line
        {
            public string Value { get; set; }

            public float Width { get; set; }
        }

        // Credit: https://forums.xamarin.com/discussion/105582/drawtext-multiline
        private Line[] SplitLines(string text, SKPaint paint)
        {
            var spaceWidth = paint.MeasureText(" ");
            var lines = text.Split('\n');

            return lines.SelectMany((line) =>
            {
                var result = new List<Line>();

                var words = line.Split(new[] { " " }, StringSplitOptions.None);

                var lineResult = new StringBuilder();
                float width = 0;
                foreach (var word in words)
                {
                    var wordWidth = paint.MeasureText(word);
                    var wordWithSpaceWidth = wordWidth + spaceWidth;
                    var wordWithSpace = word + " ";

                    lineResult.Append(wordWithSpace);
                    width += wordWithSpaceWidth;
                }

                result.Add(new Line() { Value = lineResult.ToString(), Width = width });

                return result.ToArray();
            }).ToArray();
        }

        private void RenderHorizontalAxisIntervals(SKSurface surface,
                                                   float chartStartX,
                                                   float chartStartY,
                                                   float chartEndX,
                                                   float chartEndY,
                                                   DateTime startUtc,
                                                   DateTime segmentStartUtc,
                                                   DateTime segmentEndUtc,
                                                   ITimeZone timeZone,
                                                   string dateTimeFormatString,
                                                   TelemetryTimeDisplayMode timeDisplayMode,
                                                   SKColor intervalBarColor,
                                                   SKColor intervalTextColor,
                                                   float textSize)
        {
            dateTimeFormatString = dateTimeFormatString.Replace("f", "");
            if (dateTimeFormatString.EndsWith("."))
            {
                dateTimeFormatString = dateTimeFormatString.Substring(0, dateTimeFormatString.Length - 1);
            }

            var textPaint = new SKPaint()
            {
                Color = intervalTextColor,
                TextSize = textSize,
            };

            var linePaint = new SKPaint()
            {
                IsStroke = true,
                Color = intervalBarColor,
                StrokeWidth = 2,
                PathEffect = SKPathEffect.CreateDash(new float[] { 20, 10 }, 20),
            };

            try
            {
                bool isFirst = true;
                // Draw first interval (left hand axis bar)
                for (var interval = 0.0; interval < 1.1d; interval += 0.25d)
                {
                    var text = GetIntervalText(interval, startUtc, segmentStartUtc, segmentEndUtc, timeZone, dateTimeFormatString, timeDisplayMode);
                    var positionX = (chartStartX) + (float)((chartEndX - chartStartX) * interval);
                    if (!isFirst)
                    {
                        surface.Canvas.DrawLine(positionX, chartStartY, positionX, chartEndY - 1, linePaint);
                    }

                    var textBounds = new SKRect();
                    textPaint.MeasureText(text, ref textBounds);

                    var textX = positionX - textBounds.Width / 2;
                    if (isFirst)
                    {
                        textX = positionX;
                    }

                    if (interval >= 1.0d - double.Epsilon)
                    {
                        textX = positionX - textBounds.Width;
                    }

                    surface.Canvas.DrawText(text, textX, chartEndY + textBounds.Height, textPaint);

                    isFirst = false;
                }
            }
            finally
            {
                textPaint.Dispose();
                linePaint.Dispose();
            }
        }

        string GetIntervalText(double intervalPercent,
                               DateTime startUtc,
                               DateTime segmentStartUtc,
                               DateTime segmentEndUtc,
                               ITimeZone timeZone,
                               string dateTimeFormatString,
                               TelemetryTimeDisplayMode timeDisplayMode)
        {
            if (timeDisplayMode == TelemetryTimeDisplayMode.Relative)
            {
                var startDiff = segmentStartUtc - startUtc;
                segmentStartUtc = new DateTime(startDiff.Ticks);

                var endDiff = segmentEndUtc - startUtc;
                segmentEndUtc = new DateTime(endDiff.Ticks);
            }
            else
            {
                segmentStartUtc += timeZone.Offset;
                segmentEndUtc += timeZone.Offset;
            }

            var timeSpan = segmentEndUtc - segmentStartUtc;

            var adjustedMilliseconds = timeSpan.TotalMilliseconds * intervalPercent;

            var dateTimeValue = segmentStartUtc + TimeSpan.FromMilliseconds(adjustedMilliseconds);

            return dateTimeValue.ToString(dateTimeFormatString);
        }

        private double CalculateMaxDisplayValue(IReadOnlyList<ITelemetrySegment> segments)
        {
            var maxValue = segments.Max(s => s.MaxValue);

            if (!maxValue.HasValue)
            {
                return 1.0;
            }

            var result = maxValue.Value;
            double multiplicationAmount = 1.0;
            while (result > 10.0)
            {
                result /= 10;
                multiplicationAmount *= 10.0;
            }

            result = Math.Ceiling(result);
            return result * multiplicationAmount;
        }

        private void RenderAxesLines(SKSurface surface,
                                     float chartStartX,
                                     float chartStartY,
                                     float chartEndX,
                                     float chartEndY,
                                     SKColor axesBarColor)
        {
            var paint = new SKPaint()
            {
                IsStroke = true,
                Color = axesBarColor,
                StrokeWidth = 2,
            };

            try
            {
                surface.Canvas.DrawLine(chartStartX, chartStartY, chartStartX, chartEndY, paint);
                surface.Canvas.DrawLine(chartStartX, chartEndY, chartEndX, chartEndY, paint);
            }
            finally
            {
                paint.Dispose();
            }
        }

        public ChartPosition? ConvertToChartPosition(Point viewPosition,
                                                     int chartWidth,
                                                     int chartHeight,
                                                     DateTime startUtc,
                                                     DateTime endUtc)
        {
            CalculateChartBounds(chartWidth,
                                 chartHeight,
                                 Options.TextSize,
                                 out var chartStartX,
                                 out var chartEndX,
                                 out var chartStartY,
                                 out var chartEndY);

            Point? chartPosition = new Point(viewPosition.X - chartStartX, viewPosition.Y - chartStartY);

            chartPosition = ConstrainToChartBounds(chartStartX, chartStartY, chartEndX, chartEndY, chartPosition);
            if (!chartPosition.HasValue)
            {
                return null;
            }

            var innerChartWidth = chartEndX - chartStartX;
            var interpolationPercent = chartPosition.Value.X / innerChartWidth;
            var chartPositionUtc = startUtc + new TimeSpan((long)((endUtc - startUtc).Ticks * interpolationPercent));

            return new ChartPosition(chartPosition.Value.X, chartPosition.Value.Y, chartPositionUtc);
        }
    }
}

