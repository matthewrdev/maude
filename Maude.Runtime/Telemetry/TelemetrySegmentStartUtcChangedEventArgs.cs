using System;
namespace Maude.Runtime.Telemetry
{
    public class TelemetrySegmentStartUtcChangedEventArgs : EventArgs
    {
        public TelemetrySegmentStartUtcChangedEventArgs(ITelemetrySegment telemetrySegment, DateTime oldStartUtc, DateTime newStartUtc)
        {
            TelemetrySegment = telemetrySegment ?? throw new ArgumentNullException(nameof(telemetrySegment));
            OldStartUtc = oldStartUtc;
            NewStartUtc = newStartUtc;
        }

        public ITelemetrySegment TelemetrySegment { get; }

        public DateTime OldStartUtc { get; }

        public DateTime NewStartUtc { get; }
    }
}

