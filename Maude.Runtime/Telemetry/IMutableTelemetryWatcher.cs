using System.Collections.Generic;
using Ansight.Adb.Telemetry.Sampling;

namespace Maude.Runtime.Telemetry
{
    internal interface IMutableTelemetryWatcher : ITelemetryManager
    {
        void ReceivedTelemetrySamples(string deviceId, string packageId, string channel, IReadOnlyList<ITelemetrySample> samples);
    }
}

