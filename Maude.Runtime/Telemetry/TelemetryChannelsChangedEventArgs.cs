using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
    public class TelemetryChannelsChangedEventArgs : EventArgs
    {
        public TelemetryChannelsChangedEventArgs(ITelemetrySink telementrySink, ITelemetryChannel channel)
        {
            if (channel is null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            Channels = new List<ITelemetryChannel>() { channel };
            TelementrySink = telementrySink ?? throw new ArgumentNullException(nameof(telementrySink));
        }

        public TelemetryChannelsChangedEventArgs(ITelemetrySink telementrySink, IReadOnlyList<ITelemetryChannel> channels)
        {
            TelementrySink = telementrySink ?? throw new ArgumentNullException(nameof(telementrySink));
            Channels = channels ?? throw new ArgumentNullException(nameof(channels));
        }

        /// <summary>
        /// The <see cref="ITelemetryChannel"/>'s that were changed.
        /// </summary>
        public IReadOnlyList<ITelemetryChannel> Channels { get; }
        public ITelemetrySink TelementrySink { get; }
    }
}

