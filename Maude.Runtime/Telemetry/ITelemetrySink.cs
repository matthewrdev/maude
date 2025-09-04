using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
    public interface ITelemetrySink
    {
        /// <summary>
        /// The device serial that this <see cref="ITelemetrySink"/> is for.
        /// </summary>
        string Device { get; }

        /// <summary>
        /// The identifier of the package/app that this <see cref="ITelemetrySink"/> is for.
        /// </summary>
        string PackageId { get; }

        /// <summary>
        /// The channels included in this <see cref="ITelemetrySink"/>.
        /// </summary>
        IReadOnlyList<ITelemetryChannel> Channels { get; }

        /// <summary>
        /// The names of the channels within this <see cref="ITelemetrySink"/>
        /// </summary>
        IReadOnlyList<string> ChannelNames { get; }

        /// <summary>
        /// Occurs when one or more new telemetry channels are added to this <see cref="ITelemetrySink"/>.
        /// </summary>
        event EventHandler<TelemetryChannelsChangedEventArgs> OnChannelsAdded;

        /// <summary>
        /// Occurs when one or more telemetry channels are removed from this <see cref="ITelemetrySink"/>.
        /// </summary>
        event EventHandler<TelemetryChannelsChangedEventArgs> OnChannelsRemoved;

        /// <summary>
        /// Gets the <see cref="ITelemetryChannel"/> with the given <paramref name="channelName"/> or null if no matching channel exists.
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        ITelemetryChannel GetChannel(string channelName);

        /// <summary>
        /// The minimum value of the <see cref="Channels"/> <see cref="ITelemetryChannel.StartUtc"/>
        /// </summary>
        DateTime StartUtc { get; }

        /// <summary>
        /// The maximum value of the <see cref="Channels"/> <see cref="ITelemetryChannel.EndUtc"/>
        /// </summary>
        DateTime EndUtc { get; }

        /// <summary>
        /// Occurs when the <see cref="EndUtc"/> changes.
        /// </summary>
        event EventHandler<TelemetrySinkEndUtcChangedEventArgs> OnEndUtcChanged;
    }
}

