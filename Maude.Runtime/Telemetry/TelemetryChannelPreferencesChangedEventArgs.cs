using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
    public class TelemetryChannelPreferencesChangedEventArgs : EventArgs
	{
        public TelemetryChannelPreferencesChangedEventArgs(string packageId, IReadOnlyList<string> channels)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or whitespace.", nameof(packageId));
            }

            PackageId = packageId;
            Channels = channels ?? Array.Empty<string>();
        }

        /// <summary>
        /// The 
        /// </summary>
		public string PackageId { get; }

		public IReadOnlyList<string> Channels { get; }
	}
}

