using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
    public class TelemetryExcludedGroupsPreferencesChangedEventArgs : EventArgs
    {
        public TelemetryExcludedGroupsPreferencesChangedEventArgs(string channel, IReadOnlyList<string> excludedGroups)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or whitespace.", nameof(channel));
            }

            Channel = channel;
            ExcludedGroups = excludedGroups ?? Array.Empty<string>();
        }

        /// <summary>
        /// The channel.
        /// </summary>
		public string Channel { get; }

        /// <summary>
        /// The groups that should be excluded from display.
        /// </summary>
        public IReadOnlyList<string> ExcludedGroups { get; }
    }
}

