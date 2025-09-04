using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
    /// <summary>
    /// An <see cref="EventArgs"/> that contains the <see cref="TelemetryDataPoint"/>'s that were added or removed from a given <see cref="ITelemetrySegment"/>.
    /// </summary>
    public class TelemetrySegmentDataChangedEventArgs : EventArgs
    {
        public TelemetrySegmentDataChangedEventArgs(ITelemetrySegment telemetrySegment, IReadOnlyList<TelemetryDataPoint> dataPoints)
        {
            TelemetrySegment = telemetrySegment ?? throw new ArgumentNullException(nameof(telemetrySegment));
            DataPoints = dataPoints ?? throw new ArgumentNullException(nameof(dataPoints));
        }

        /// <summary>
        /// The <see cref="ITelemetrySegment"/> who had new <see cref="TelemetryDataPoint"/>'s added or removed.
        /// </summary>
        public ITelemetrySegment TelemetrySegment { get; }

        /// <summary>
        /// The <see cref="TelemetryDataPoint"/>'s that were added or removed.
        /// </summary>
        public IReadOnlyList<TelemetryDataPoint> DataPoints { get; }
    }
}

