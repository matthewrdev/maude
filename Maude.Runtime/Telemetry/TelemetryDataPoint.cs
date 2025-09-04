using System;
using System.Diagnostics;

namespace Maude.Runtime.Telemetry
{
    /// <summary>
    /// A singular data point for an <see cref="ITelemetrySegment"/> and <see cref="ITelemetryChannel"/>.
    /// </summary>
    [DebuggerDisplay("Value={Value},Position={DateTimeUtc}")]
    public struct TelemetryDataPoint
    {
        public TelemetryDataPoint(DateTime dateTimeUtc,
                                  double value,
                                  string data = "")
        {
            DateTimeUtc = dateTimeUtc;
            Value = value;
            Data = data;
        }

        /// <summary>
        /// The date and time in the UTC timezone that this <see cref="TelemetryDataPoint"/> was captured at.
        /// </summary>
        public DateTime DateTimeUtc { get; }

        /// <summary>
        /// The raw value of this data-point.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Additional data attached to this telemetry data point.
        /// </summary>
        public string Data { get; }
    }
}

