using System;
using System.Collections.Generic;
using Ansight.Adb.Devices;
using Ansight.Adb.Telemetry.Sampling;

namespace Maude.Runtime.Telemetry.Sampling
{
	public class TelemetrySamplesEventArgs : EventArgs
	{
        public TelemetrySamplesEventArgs(string deviceId,
                                         string packageId,
                                         string channel,
                                         IReadOnlyList<ITelemetrySample> samples)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or empty.", nameof(packageId));
            }

            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or empty.", nameof(channel));
            }

            DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            PackageId = packageId;
            Channel = channel;
            Samples = samples ?? throw new ArgumentNullException(nameof(samples));
        }

		public string DeviceId { get; }

		public string PackageId { get; }

		public string Channel { get; }

		public IReadOnlyList<ITelemetrySample> Samples { get; }
	}
}

