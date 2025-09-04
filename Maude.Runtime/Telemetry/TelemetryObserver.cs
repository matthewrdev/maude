using System;
using System.Collections.Generic;
using System.Linq;
using Ansight.Adb.Telemetry.Sampling;
using Ansight.Concurrency;

namespace Maude.Runtime.Telemetry
{
    /// <summary>
    /// Manages the connection of <see cref="ITelemetryStream"/>s and their sampling to the <see cref="ITelemetryManager"/>
    /// </summary>
    internal class TelemetryObserver
    {
        public TelemetryObserver(string deviceId,
                                 string packageId,
                                 TimeSpan deviceTimeOffset,
                                 IMutableTelemetrySink telemetrySink,
                                 ITelemetryStreamFactoryRepository telemetryStreamFactories,
                                 IMutableTelemetryWatcher telemetryManager,
                                 IReadOnlyList<string> channels)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentException($"'{nameof(deviceId)}' cannot be null or empty.", nameof(deviceId));
            }

            if (string.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or empty.", nameof(packageId));
            }

            DeviceId = deviceId;
            PackageId = packageId;
            DeviceTimeOffset = deviceTimeOffset;
            TelemetrySink = telemetrySink ?? throw new ArgumentNullException(nameof(telemetrySink));
            this.telemetryStreamFactories = telemetryStreamFactories ?? throw new ArgumentNullException(nameof(telemetryStreamFactories));
            this.telemetryManager = telemetryManager ?? throw new ArgumentNullException(nameof(telemetryManager));
            this.channels.Set(channels?.ToList() ?? new List<string>());
        }


        public IMutableTelemetrySink TelemetrySink { get; }
        private readonly ITelemetryStreamFactoryRepository telemetryStreamFactories;
        private readonly IMutableTelemetryWatcher telemetryManager;

        public string DeviceId { get; }

        public string PackageId { get; }

        public TimeSpan DeviceTimeOffset { get; }

        private readonly ConcurrentValue<List<string>> channels = new ConcurrentValue<List<string>>(new List<string>());
        public IReadOnlyList<string> Channels => channels.Get(value => value.ToList()); // Create a shallow copy.

        private readonly ConcurrentValue<List<ITelemetryStream>> telemetryStreams = new ConcurrentValue<List<ITelemetryStream>>(new List<ITelemetryStream>());
        public IReadOnlyList<ITelemetryStream> TelemetryStreams => telemetryStreams.Get(value => value.ToList()); // Create a shallow copy.

        private readonly ConcurrentValue<bool> isRunning = new ConcurrentValue<bool>(false);

        public bool IsRunning => isRunning.Get();

        public void AddChannel(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or whitespace.", nameof(channel));
            }

            // Does this channel already exist?
            if (channels.Get(c => c.Contains(channel)))
            {
                // Ignore the request to add the channel
                return;
            }

            var factory = this.telemetryStreamFactories.GetStreamFactoryForChannel(channel);
            if (factory is null)
            {
                // Unsupported channel/soure
                return;
            }

            this.channels.Mutate(c => c.Add(channel));

            if (this.IsRunning)
            {
                var stream = factory.Start(this.DeviceId, this.PackageId, this.DeviceTimeOffset);
                telemetryStreams.Mutate(ts => ts.Add(stream));

                stream.OnNewTelemetrySamples += Stream_OnNewTelemetrySamples;
            }
        }

        private void Stream_OnNewTelemetrySamples(object sender, TelemetrySamplesEventArgs e)
        {
            this.telemetryManager.ReceivedTelemetrySamples(e.DeviceId, e.PackageId, e.Channel, e.Samples);
        }

        public void RemoveChannel(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or whitespace.", nameof(channel));
            }
        }

        internal void Start()
        {
            if (IsRunning)
            {
                return;
            }

            foreach (var channel in Channels)
            {
                var factory = this.telemetryStreamFactories.GetStreamFactoryForChannel(channel);
                if (factory is null)
                {
                    continue;
                }

                var stream = factory.Start(this.DeviceId, this.PackageId, this.DeviceTimeOffset);
                stream.OnNewTelemetrySamples += Stream_OnNewTelemetrySamples;
                this.telemetryStreams.Mutate(streams => streams.Add(stream));
            }


            this.isRunning.Set(true);
        }

        internal void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            this.isRunning.Set(false);

            var streams = telemetryStreams.Get();
            telemetryStreams.Set(new List<ITelemetryStream>());
            foreach (var stream in streams)
            {
                var streamFactory = telemetryStreamFactories.GetStreamFactoryForChannel(stream.Channel);
                if (streamFactory is null)
                {
                    // Should never happen, sanity check.
                    continue;
                }

                stream.OnNewTelemetrySamples -= this.Stream_OnNewTelemetrySamples;
                streamFactory.Stop(stream);
            }
        }
    }
}