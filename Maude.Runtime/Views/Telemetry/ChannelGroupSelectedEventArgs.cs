using System;
namespace Maude.Runtime.Views.Telemetry
{
	public class ChannelGroupSelectedEventArgs : EventArgs
	{
		public ChannelGroupSelectedEventArgs(string channel,
                                             string group,
                                             bool isGroupEnabled)
		{
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or whitespace.", nameof(channel));
            }

            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or whitespace.", nameof(group));
            }

            Channel = channel;
            Group = group;
            IsGroupEnabled = isGroupEnabled;
        }

        public string Channel { get; }

        public string Group { get; }

        public bool IsGroupEnabled { get; }
    }
}

