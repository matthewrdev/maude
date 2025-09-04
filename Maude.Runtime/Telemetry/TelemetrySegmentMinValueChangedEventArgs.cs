using System;
namespace Maude.Runtime.Telemetry
{
    public class TelemetrySegmentMinValueChangedEventArgs : EventArgs
    {
        public TelemetrySegmentMinValueChangedEventArgs(ITelemetrySegment telemetrySegment, double? oldMinValue, double? newMinValue)
        {
            TelemetrySegment = telemetrySegment ?? throw new ArgumentNullException(nameof(telemetrySegment));
            OldMinValue = oldMinValue;
            NewMinValue = newMinValue;
        }

        public ITelemetrySegment TelemetrySegment { get; }

        public double? OldMinValue { get; }

        public double? NewMinValue { get; }
    }
}

