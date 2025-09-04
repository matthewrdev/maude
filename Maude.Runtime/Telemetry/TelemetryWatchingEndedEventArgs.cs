using System;
namespace Maude.Runtime.Telemetry
{
    public class TelemetryWatchingEndedEventArgs : EventArgs
    {
        public TelemetryWatchingEndedEventArgs(ITelemetrySink telemetrySink, TelemetryWatchingEndedReason reason)
        {
            TelemetrySink = telemetrySink ?? throw new ArgumentNullException(nameof(telemetrySink));
            Reason = reason;
        }

        public ITelemetrySink TelemetrySink { get; }

        public TelemetryWatchingEndedReason Reason { get; }
    }
}

