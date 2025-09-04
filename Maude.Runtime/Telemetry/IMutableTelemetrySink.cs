using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
    public interface IMutableTelemetrySink : ITelemetrySink
    {
        bool IsEditable { get; }

        void CloseEditing();

        IMutableTelemetryChannel CreateChannel(string channelName);

        IMutableTelemetryChannel GetEditableChannel(string channelName);

        void CloseActiveSegments();

        /// <summary>
        /// Closes and clears all active telemetry channels.
        /// </summary>
        //void Reset();
    }
}

