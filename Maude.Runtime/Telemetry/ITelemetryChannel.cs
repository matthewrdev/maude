using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
    public interface ITelemetryChannel
    {
        /// <summary>
        /// The <see cref="ITelemetrySink"/> that owns this <see cref="ITelemetryChannel"/>.
        /// </summary>
        ITelemetrySink Sink { get; }

        /// <summary>
        /// The name of this channel.
        /// <para/>
        /// This is the data kind that this 
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The minimum value of the <see cref="Segments"/> <see cref="ITelemetrySegment.StartUtc"/> values.
        /// </summary>
        DateTime StartUtc { get; }

        /// <summary>
        /// The maxumum value of the <see cref="Segments"/> <see cref="ITelemetrySegment.EndUtc"/> values.
        /// </summary>
        DateTime EndUtc { get; }

        /// <summary>
        /// The segment group names within this <see cref="ITelemetryChannel"/>.
        /// </summary>
        IReadOnlyList<string> Groups { get; }

        /// <summary>
        /// The <see cref="ITelemetrySegment"/>'s within this <see cref="ITelemetryChannel"/>.
        /// <para/>
        /// <see cref="ITelemetrySegment"/>
        /// </summary>
        IReadOnlyList<ITelemetrySegment> Segments { get; }

        /// <summary>
        /// Get the <see cref="ITelemetrySegment"/>'s that whose <see cref="ITelemetrySegment.Group"/> matches the provided <paramref name="group"/>.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        IReadOnlyList<ITelemetrySegment> GetSegmentsForGroup(string group);

        /// <summary>
        /// Retri
        /// </summary>
        /// <param name="startUtc"></param>
        /// <param name="endUtc"></param>
        /// <returns></returns>
        IReadOnlyList<ITelemetrySegment> GetSegmentsInRange(DateTime startUtc, DateTime endUtc);

        /// <summary>
        /// Get the <see cref="ITelemetrySegment"/>'s that whose <see cref="ITelemetrySegment.Group"/> matches the provided <paramref name="group"/> in the range.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>

        IReadOnlyList<ITelemetrySegment> GetSegmentsInRangeForGroup(string groupName, DateTime startUtc, DateTime endUtc);

        /// <summary>
        /// Occurs when a one or more <see cref="ITelemetrySegment"/>'s are 'opened' for this channel.
        /// <para/>
        /// An <see cref="ITelemetrySegment"/> can be added either when the telemetry sink opens and creates the initial samples OR when a new process for the targeted package starts.
        /// </summary>
        event EventHandler<TelemetrySegmentsChangedEventArgs> OnTelemetrySegmentsOpened;

        /// <summary>
        /// Occurs when one or more <see cref="ITelemetrySegment"/>'s are closed in this channel.
        /// <para/>
        /// An <see cref="ITelemetrySegment"/> is typically closed when the device is still connected however the targeted package process stops.
        /// </summary>
        event EventHandler<TelemetrySegmentsChangedEventArgs> OnTelemetrySegmentsClosed;

        /// <summary>
        /// Occurs when the <see cref="EndUtc"/> changes.
        /// </summary>
        event EventHandler<TelemetryChannelEndUtcChangedEventArgs> OnEndUtcChanged;

    }
}

