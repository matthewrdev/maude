using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
    public class TelemetrySegmentsChangedEventArgs : EventArgs
    {
        public TelemetrySegmentsChangedEventArgs(ITelemetrySegment segment)
        {
            if (segment is null)
            {
                throw new ArgumentNullException(nameof(segment));
            }

            Segments = new List<ITelemetrySegment>() { segment };
        }

        public TelemetrySegmentsChangedEventArgs(IReadOnlyList<ITelemetrySegment> segments)
        {
            Segments = segments ?? throw new ArgumentNullException(nameof(segments));
        }

        public IReadOnlyList<ITelemetrySegment> Segments { get; }
    }
}

