using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry.Sampling
{
    public interface ITelemetryStreamFactory
    {
        /// <summary>
        /// The channel name that this source creates data for.
        /// </summary>
        string Channel { get; }

        /// <summary>
        /// Opens a new telemetry stream targetting the <paramref name="deviceId"/> and <paramref name="packageId"/>
        /// </summary>
        /// <param name="device"></param>
        /// <param name="packageId"></param>
        /// <returns></returns>
        ITelemetryStream Start(string deviceId, string packageId, TimeSpan deviceTimeOffset);

        /// <summary>
        /// Stops the given telemetry stream.
        /// </summary>
        /// <param name="telemetryStream"></param>
        void Stop(ITelemetryStream telemetryStream);

        /// <summary>
        /// Stops all telem
        /// </summary>
        void StopAll();

        IReadOnlyList<ITelemetryStream> ActiveTelemetryStreams { get; }
    }
}

