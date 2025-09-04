using System;
namespace Maude.Runtime.Telemetry
{
    public class TelemetrySinkEndUtcChangedEventArgs : EventArgs
    {
        public TelemetrySinkEndUtcChangedEventArgs(ITelemetrySink telemetrySink, DateTime oldEndUtc, DateTime newEndUtc)
        {
            TelemetrySink = telemetrySink ?? throw new ArgumentNullException(nameof(telemetrySink));
            OldEndUtc = oldEndUtc;
            NewEndUtc = newEndUtc;
        }

        public ITelemetrySink TelemetrySink { get; }

        public DateTime OldEndUtc { get; }

        public DateTime NewEndUtc { get; }
    }
}

