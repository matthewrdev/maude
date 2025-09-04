using System;
using System.Collections.Generic;
using System.Linq;

namespace Maude.Runtime.Telemetry.Sampling;

public class TelemetrySample : ITelemetrySample
{
    public TelemetrySample(string group,
        IReadOnlyList<TelemetryDataPoint> data)
    {
        if (string.IsNullOrEmpty(group))
        {
            throw new ArgumentException($"'{nameof(group)}' cannot be null or empty.", nameof(group));
        }

        if (!data.Any())
        {
            throw new InvalidOperationException($"The {nameof(data)} provided to the telemetry sample '{group}' was empty.");
        }

        Group = group;
        CapturedAtUtc = data.Min(d => d.DateTimeUtc);
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public TelemetrySample(string group,
        TelemetryDataPoint data,
        string additionalData = "")
        : this(group, new List<TelemetryDataPoint> { data })
    {
    }

    public TelemetrySample(string group,
        DateTime capturedAtUtc,
        double value,
        string additionalData = "")
    {
        if (string.IsNullOrEmpty(group))
        {
            throw new ArgumentException($"'{nameof(group)}' cannot be null or empty.", nameof(group));
        }

        Group = group;
        CapturedAtUtc = capturedAtUtc;
        Data = new List<TelemetryDataPoint>()
        {
            new TelemetryDataPoint(capturedAtUtc, value, data:additionalData)
        };
    }

    public string Group { get; }

    public DateTime CapturedAtUtc { get; }

    public IReadOnlyList<TelemetryDataPoint> Data { get; }
}