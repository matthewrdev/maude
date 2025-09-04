using System;
namespace Maude.Runtime.Telemetry
{
    public class TelemetryWatchingStartedEventArgs : EventArgs
    {
        public TelemetryWatchingStartedEventArgs(ITelemetrySink telemetrySink)
        {
            TelemetrySink = telemetrySink ?? throw new ArgumentNullException(nameof(telemetrySink));
        }

        public ITelemetrySink TelemetrySink { get; }
    }
}

