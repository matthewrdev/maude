using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
    /// <summary>
    /// An see <see cref="ITelemetrySegment"/> that can has its <see cref="Data"/> changed.
    /// </summary>
    public interface IMutableTelemetrySegment : ITelemetrySegment
    {
        /// <summary>
        /// If this <see cref="IMutableTelemetrySegment"/> accepts new data.
        /// </summary>
        bool IsEditable { get; }

        /// <summary>
        /// Closes editing on this <see cref="ITelemetrySegment"/>.
        /// <para/>
        /// After closing for editing, all future <see cref="AddData(IReadOnlyList{TelemetryDataPoint})"/> invocations will throw an exception.
        /// </summary>
        void CloseEditing();

        /// <summary>
        /// Add the <paramref name="dataPoint"/> into the <see cref="ITelemetrySegment.Data"/>.
        /// </summary>
        /// <param name="dataPoints"></param>
        void AddData(TelemetryDataPoint dataPoint);

        /// <summary>
        /// Add the <paramref name="dataPoints"/> into the <see cref="ITelemetrySegment.Data"/>.
        /// <para/>
        /// Locates the min, max and end utc values of the set and applies sorting to the incoming <paramref name="dataPoints"/>.
        /// <para/>
        /// To avoid this computation, use <see cref="AddData(List{TelemetryDataPoint}, double, double, DateTime)"/>
        /// </summary>
        /// <param name="dataPoints"></param>
        void AddData(IReadOnlyList<TelemetryDataPoint> dataPoints);

        /// <summary>
        /// Add the <paramref name="telemetryDataPoints"/> into the <see cref="ITelemetrySegment.Data"/>.
        /// </summary>
        /// <param name="telemetryDataPoints">The <see cref="TelemetryDataPoint"/>'s to add</param>
        /// <param name="minValue">The minimum value of the data points.</param>
        /// <param name="maxValue">The maximum value of the data points.</param>
        /// <param name="endUtc">The end UTC value of the data points.</param>
        void AddData(IReadOnlyList<TelemetryDataPoint> dataPoints, double minValue, double maxValue, DateTime endUtc);

        /// <summary>
        /// Removes the <paramref name="telemetryDataPoints"/> from the <see cref="ITelemetrySegment.Data"/>.
        /// </summary>
        /// <param name="telemetryDataPoints">The <see cref="TelemetryDataPoint"/>'s to remove</param>
        void RemoveData(IReadOnlyList<TelemetryDataPoint> dataPoints);

        /// <summary>
        /// Removes any <see cref="TelemetryDataPoint"/> from the <see cref="ITelemetrySegment.Data"/> that match the <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">The predicate to use to </param>
        void RemoveData(Func<TelemetryDataPoint, bool> predicate);

        /// <summary>
        /// Removes any <see cref="TelemetryDataPoint"/> from the <see cref="ITelemetrySegment.Data"/> that occurs before the <paramref name="dateTimeUtc"/>.
        /// </summary>
        /// <param name="predicate">The predicate to </param>
        void RemoveBefore(DateTime dateTimeUtc);

        /// <summary>
        /// Removes any <see cref="TelemetryDataPoint"/> from the <see cref="ITelemetrySegment.Data"/> that occurs after the <paramref name="dateTimeUtc"/>.
        /// </summary>
        /// <param name="predicate">The predicate to </param>
        void RemoveAfter(DateTime dateTimeUtc);

        /// <summary>
        /// Clears all data within this telemetry segment.
        /// </summary>
        void Clear();

        /// <summary>
        /// Recomputes the <see cref="ITelemetrySegment.StartUtc"/>,  <see cref="ITelemetrySegment.EndUtc"/>,  <see cref="ITelemetrySegment.MinValue"/> and  <see cref="ITelemetrySegment.MaxValue"/> values for this <see cref="ITelemetrySegment"/>.
        /// </summary>
        void Recalculate();
    }
}

