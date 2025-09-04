using System;
namespace Maude.Runtime.Telemetry
{
    public class  TelemetrySegmentMaxValueChangedEventArgs : EventArgs
    {
        public TelemetrySegmentMaxValueChangedEventArgs(ITelemetrySegment telemetrySegment, double? oldMaxValue, double? newMaxValue)
        {
            TelemetrySegment = telemetrySegment ?? throw new ArgumentNullException(nameof(telemetrySegment));
            OldMaxValue = oldMaxValue;
            NewMaxValue = newMaxValue;
        }

        public ITelemetrySegment TelemetrySegment { get; }

        public double? OldMaxValue { get; }

        public double? NewMaxValue { get; }
    }
}

