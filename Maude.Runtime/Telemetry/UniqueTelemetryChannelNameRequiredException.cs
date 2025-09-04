using System;

namespace Maude.Runtime.Telemetry
{
    public class UniqueTelemetryChannelNameRequiredException : Exception
    {
        public UniqueTelemetryChannelNameRequiredException(ITelemetrySink sink, string channelName)
            : base($"The telemtry sink for {sink.Device} and {sink.PackageId} is already contains a channel named '{channelName}'.")
        {
        }
    }
}

