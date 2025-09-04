using System;
namespace Maude.Runtime.Telemetry
{
    public class TelemetrySegmentEndUtcChangedEventArgs : EventArgs
    {
        public TelemetrySegmentEndUtcChangedEventArgs(ITelemetrySegment telemetrySegment, DateTime oldEndUtc, DateTime newEndUtc)
        {
            TelemetrySegment = telemetrySegment ?? throw new ArgumentNullException(nameof(telemetrySegment));
            OldEndUtc = oldEndUtc;
            NewEndUtc = newEndUtc;
        }

        public ITelemetrySegment TelemetrySegment { get; }

        public DateTime OldEndUtc { get; }

        public DateTime NewEndUtc { get; }
    }
}

