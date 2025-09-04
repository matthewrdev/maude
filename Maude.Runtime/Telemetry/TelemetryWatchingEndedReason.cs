using System;
namespace Maude.Runtime.Telemetry
{
    public enum TelemetryWatchingEndedReason 
    {
        /// <summary>
        /// Signals that the telemetry watching was stopped by user request.
        /// </summary>
        UserRequested,

        /// <summary>
        /// Signals that the telemetry watching was stopped as the device was disconnected.
        /// </summary>
        DeviceDisconnected,

        /// <summary>
        /// Signals that the telemetry watching was stopped as the product is shutting down.
        /// </summary>
        ApplicationStopped,

        /// <summary>
        /// Signals that the telemetry watching was stopped for an unknown reason.
        /// </summary>
        Unknown,
    }
}

