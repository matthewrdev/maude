using System;
namespace Maude.Runtime.Telemetry
{
    public class TelemetryChannelEndUtcChangedEventArgs : EventArgs
    {
        public TelemetryChannelEndUtcChangedEventArgs(ITelemetryChannel telemetryChannel, DateTime oldEndUtc, DateTime newEndUtc)
        {
            TelemetryChannel = telemetryChannel ?? throw new ArgumentNullException(nameof(telemetryChannel));
            OldEndUtc = oldEndUtc;
            NewEndUtc = newEndUtc;
        }

        public ITelemetryChannel TelemetryChannel { get; }

        public DateTime OldEndUtc { get; }

        public DateTime NewEndUtc { get; }
    }
}

