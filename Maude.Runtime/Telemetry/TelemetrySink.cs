using System;
using System.Collections.Generic;
using System.Linq;
using Ansight.Adb.Telemetry;
using Ansight.Concurrency;

namespace Maude.Runtime.Telemetry
{
    public class TelemetrySink : IMutableTelemetrySink
    {
        readonly Logging.ILogger log;

        public TelemetrySink(string device,
                             string packageId,
                             bool isEditable)
        {
            if (string.IsNullOrWhiteSpace(device))
            {
                throw new ArgumentException($"'{nameof(device)}' cannot be null or whitespace.", nameof(device));
            }

            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or whitespace.", nameof(packageId));
            }

            Device = device;
            PackageId = packageId;
            log = Logging.Logger.Create(nameof(TelemetrySink) + $":{Device}:{PackageId}");
            this.isEditable.Set(isEditable);
        }

        public TelemetrySink(string device,
                             string packageId,
                             IReadOnlyList<IMutableTelemetryChannel> channels)
        {
            if (string.IsNullOrWhiteSpace(device))
            {
                throw new ArgumentException($"'{nameof(device)}' cannot be null or whitespace.", nameof(device));
            }

            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or whitespace.", nameof(packageId));
            }

            Device = device;
            PackageId = packageId;
            this.channels.Mutate(c => c.AddRange(channels));
            this.endUtc.Set(channels.Max(c => c.EndUtc));
            this.isEditable.Set(false);
        }

        public string Device { get; }

        public string PackageId { get; }

        private readonly ConcurrentValue<bool> isEditable = new ConcurrentValue<bool>(true);
        public bool IsEditable => isEditable.Get();

        private readonly ConcurrentValue<List<IMutableTelemetryChannel>> channels = new ConcurrentValue<List<IMutableTelemetryChannel>>(new List<IMutableTelemetryChannel>());
        public IReadOnlyList<ITelemetryChannel> Channels => this.channels.Get(c => c.ToList()); // Create a shallow copy.

        public IReadOnlyList<string> ChannelNames => channels.Get(value => value.Select(c => c.Name).ToList());

        public DateTime StartUtc => Channels.Min(c => c.StartUtc);

        private readonly ConcurrentValue<DateTime> endUtc = new ConcurrentValue<DateTime>(DateTime.MinValue);
        public DateTime EndUtc => endUtc.Get();

        public event EventHandler<TelemetryChannelsChangedEventArgs> OnChannelsAdded;
        public event EventHandler<TelemetryChannelsChangedEventArgs> OnChannelsRemoved;
        public event EventHandler<TelemetrySinkEndUtcChangedEventArgs> OnEndUtcChanged;

        public void CloseEditing()
        {
            isEditable.Set(false);

            this.channels.Mutate(channels =>
            {
                foreach (var channel in channels)
                {
                    if (channel.IsEditable)
                    {
                        channel.CloseEditing();
                        channel.OnEndUtcChanged -= Channel_OnEndUtcChanged;
                    }
                }
            });
        }

        public IMutableTelemetryChannel CreateChannel(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                throw new ArgumentException($"'{nameof(channelName)}' cannot be null or whitespace.", nameof(channelName));
            }

            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            var existingChannel = GetEditableChannel(channelName);
            if (existingChannel != null)
            {
                log.Info($"Attempted to create a telemetry channel '{channelName}' however the channel already exists.");
                return existingChannel;
            }

            var channel = new TelemetryChannel(channelName, this, isEditable: true);
            channel.OnEndUtcChanged += Channel_OnEndUtcChanged;

            this.channels.Mutate(value => value.Add(channel));
            this.OnChannelsAdded?.Invoke(this, new TelemetryChannelsChangedEventArgs(this, channel));

            return channel;
        }

        public IMutableTelemetryChannel GetEditableChannel(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                throw new ArgumentException($"'{nameof(channelName)}' cannot be null or whitespace.", nameof(channelName));
            }

            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            return GetChannel(channelName) as IMutableTelemetryChannel;
        }

        public ITelemetryChannel GetChannel(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                throw new ArgumentException($"'{nameof(channelName)}' cannot be null or whitespace.", nameof(channelName));
            }

            return this.channels.Get(values => values.FirstOrDefault(c => c.Name == channelName));
        }

        public static ITelemetrySink Empty(string deviceId, string packageId = "")
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                throw new ArgumentException($"'{nameof(deviceId)}' cannot be null or empty.", nameof(deviceId));
            }

            return new TelemetrySink(deviceId, string.IsNullOrWhiteSpace(packageId) ? "NO-PACKAGE" : packageId, false);
        }

        public void CloseActiveSegments()
        {
            if (IsEditable == false)
            {
                throw new TelemetryEditingClosedException(this);
            }

            foreach (var channelName in ChannelNames)
            {
                var channel = GetEditableChannel(channelName);
                if (channel.IsEditable == false)
                {
                    continue;
                }

                channel.CloseSegmentForGroups(channel.Groups);
            }
        }

        private void Channel_OnEndUtcChanged(object sender, TelemetryChannelEndUtcChangedEventArgs e)
        {
            if (e.NewEndUtc > this.EndUtc)
            {
                var old = this.EndUtc;
                endUtc.Set(e.NewEndUtc);
                OnEndUtcChanged?.Invoke(this, new TelemetrySinkEndUtcChangedEventArgs(this, old, e.NewEndUtc));
            }
        }

    }
}