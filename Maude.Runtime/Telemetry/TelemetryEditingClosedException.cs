using System;
namespace Maude.Runtime.Telemetry
{
    public class TelemetryEditingClosedException : Exception 
    {
        public TelemetryEditingClosedException(ITelemetrySegment segment)
            : base($"The telemtry segment {segment.Group} (Start={segment.StartUtc}, End={segment.EndUtc}) is closed for editing.")
        {
        }

        public TelemetryEditingClosedException(ITelemetryChannel channel)
            : base($"The telemtry channel for {channel.Name} is closed for editing.")
        {
        }

        public TelemetryEditingClosedException(ITelemetrySink sink)
            : base($"The telemtry sink for {sink.Device} and {sink.PackageId} is closed for editing.")
        {
        }
    }
}

