using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
    public interface ITelemetrySegment
    {
        /// <summary>
        /// The <see cref="ITelemetryChannel"/> that owns this <see cref="ITelemetrySegment"/>.
        /// </summary>
        ITelemetryChannel Channel { get; }

        /// <summary>
        /// The group that this data set belongs to within the parent <see cref="ITelemetryChannel"/>.
        /// <para/>
        /// As telemetry monitoring can start and stop, a represented value (such as views, CPU%, memory) can have distinct segments.
        /// </summary>
        string Group { get; }

        /// <summary>
        /// The unique identifier of this telemetry segment.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// The data represented by this telemetry segment.
        /// </summary>
        IReadOnlyList<TelemetryDataPoint> Data { get; }

        /// <summary>
        /// If this <see cref="ITelemetrySegment"/> has <see cref="Data"/>.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// The date time in UTC that this <see cref="ITelemetrySegment"/> starts.
        /// </summary>
        DateTime StartUtc { get; }

        /// <summary>
        /// The date time in UTC that this <see cref="ITelemetrySegment"/> ends.
        /// </summary>
        DateTime EndUtc { get; }

        /// <summary>
        /// The minimum <see cref="TelemetryDataPoint.Value"/> within this <see cref="ITelemetrySegment"/>'s <see cref="Data"/>.
        /// <para/>
        /// If there is no <see cref="Data"/> in this segment, the <see cref="MinValue"/> will be null.
        /// </summary>
        double? MinValue { get; }

        /// <summary>
        /// The maximum <see cref="TelemetryDataPoint.Value"/> within this <see cref="ITelemetrySegment"/>'s <see cref="Data"/>.
        /// <para/>
        /// If there is no <see cref="Data"/> in this segment, the <see cref="MaxValue"/> will be null.
        /// </summary>
        double? MaxValue { get; }

        /// <summary>
        /// Occurs when one or more new <see cref="TelemetryDataPoint"/>'s are added to this segment.
        /// </summary>
        event EventHandler<TelemetrySegmentDataChangedEventArgs> OnDataPointsAdded;

        /// <summary>
        /// Occurs when one or more <see cref="TelemetryDataPoint"/>'s are removed from this segment.
        /// </summary>
        event EventHandler<TelemetrySegmentDataChangedEventArgs> OnDataPointsRemoved;

        /// <summary>
        /// Occurs when the <see cref="MinValue"/> of this <see cref="ITelemetrySegment"/> changes.
        /// </summary>
        event EventHandler<TelemetrySegmentMinValueChangedEventArgs> OnMinValueChanged;

        /// <summary>
        /// Occurs when the <see cref="MinValue"/> of this <see cref="ITelemetrySegment"/> changes.
        /// </summary>
        event EventHandler<TelemetrySegmentMaxValueChangedEventArgs> OnMaxValueChanged;

        /// <summary>
        /// Occurs when the <see cref="EndUtc"/> value of this <see cref="ITelemetrySegment"/> changes.
        /// <para/>
        /// Typically corresponds to the <see cref="OnDataPointsAdded"/> event.
        /// </summary>
        event EventHandler<TelemetrySegmentEndUtcChangedEventArgs> OnEndUtcChanged;

        /// <summary>
        /// Occurs when the <see cref="StartUtc"/> value of this <see cref="ITelemetrySegment"/> changes.
        /// <para/>
        /// Typically corresponds to the <see cref="OnDataPointsRemoved"/> event.
        /// </summary>
        event EventHandler<TelemetrySegmentStartUtcChangedEventArgs> OnStartUtcChanged;

        /// <summary>
        /// Retrieves all data points within the provided range.
        /// </summary>
        /// <param name="startUtc"></param>
        /// <param name="endUtc"></param>
        /// <returns></returns>
        IReadOnlyList<TelemetryDataPoint> GetPointsInRange(DateTime startUtc, DateTime endUtc);
    }
}

