using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry.Sampling
{
    public interface ITelemetrySample
    {
        string Group { get; }

        DateTime CapturedAtUtc { get; }

        IReadOnlyList<TelemetryDataPoint> Data { get; }
    }
}

