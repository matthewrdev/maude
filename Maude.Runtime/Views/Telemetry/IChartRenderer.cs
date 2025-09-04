using System;
using Ansight.Adb.Telemetry;
using Ansight.Studio.Telemetry;
using Ansight.TimeZones;
using SkiaSharp;

namespace Maude.Runtime.Views.Telemetry
{
    /// <summary>
    /// Renders the contents of an <see cref="ITelemetryChannel"/> onto a SkiaSharp <see cref="SKSurface"/>.
    /// </summary>
    public interface IChartRenderer : IDisposable
    {
        IChartRenderingOptions Options { get; }

        void Render(SKSurface surface,
                    int canvasWidth,
                    int canvasHeight,
                    DateTime position,
                    bool shouldShowPosition,
                    Point? hoverPosition,
                    ITimeZone timeZone,
                    DateTime startTimeUtc,
                    TelemetryTimeDisplayMode timeDisplayMode,
                    string axisLabel,
                    string dateTimeFormatString,
                    int verticalAxisIntervalAmount,
                    ITelemetryChannel telemetryChannel,
                    IReadOnlyList<string> excludedGroups);


        void CalculateDisplayAreaStartEndDateTimes(DateTime position,
                                                   DateTime startTimeUtc,
                                                   TelemetryTimeDisplayMode timeDisplayMode,
                                                   out DateTime segmentStartUtc,
                                                   out DateTime segmentEndUtc);

        // TODO: Selection range rendering?
        // TODO: The calcualtion of values for the chart rendering should be 

        /// <summary>
        /// Converts the given <paramref name="viewPosition"/> into chart's value space.
        /// <para/>
        /// May return null if the position is not within the chart
        /// </summary>
        /// <param name="chartWidth"></param>
        /// <param name="chartHeight"></param>
        /// <param name="viewPosition"></param>
        /// <returns></returns>
        ChartPosition? ConvertToChartPosition(Point viewPosition,
                                              int chartWidth,
                                              int chartHeight,
                                              DateTime startUtc,
                                              DateTime endUtc);
    }
}

