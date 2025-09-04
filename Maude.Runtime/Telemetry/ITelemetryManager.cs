using System;
using System.Collections.Generic;
using Ansight.Adb.Devices;

namespace Maude.Runtime.Telemetry
{
    public interface ITelemetryManager
    {
        /// <summary>
        /// Occurs when the <see cref="ITelemetrySink"/> starts being watched for a <see cref="IDevice"/> and package.
        /// </summary>
        event EventHandler<TelemetryWatchingStartedEventArgs> StartedWatchingTelemetry;

        /// <summary>
        /// Occurs when the <see cref="ITelemetrySink"/> is stopped being watched for a <see cref="IDevice"/> and package.
        /// </summary>
        event EventHandler<TelemetryWatchingEndedEventArgs> StoppedWatchingTelemetry;

        /// <summary>
        /// The active telemetry sinks that are monitoring a device and package.
        /// </summary>
        IReadOnlyList<ITelemetrySink> TelemetrySinks { get; }

        /// <summary>
        /// Get's if the given <paramref name="device"/> has an active <see cref="ITelemetrySink"/>.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        bool IsWatchingTelemetry(IDevice device, string packageId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="device"></param>
        /// <param name="packageId"></param>
        /// <returns></returns>
        IReadOnlyList<string> GetWatchingTelemetryChannels(IDevice device, string packageId);

        /// <summary>
        /// Get the active <see cref="ITelemetrySink"/> stream for the <paramref name="device"/> and <paramref name="packageId"/>.
        /// </summary>
        ITelemetrySink GetTelemetrySink(IDevice device, string packageId);

        /// <summary>
        /// Starts watching the telemetry for the given <paramref name="device"/> and <paramref name="packageId"/>.
        /// <para/>
        /// If the telemetry is already being watched for the given device and package, returns the current sink.
        /// </summary>
        ITelemetrySink StartWatching(IDevice device, string packageId, IReadOnlyList<string> channels);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="device"></param>
        /// <param name="packageID"></param>
        /// <param name="channelName"></param>
        void ActivateTelemetryChannel(IDevice device, string packageID, string channel);

        void DeactivateTelemetryChannel(IDevice device, string packageID, string channel);

        void ApplyTelemetryChannels(IDevice device, string packageID, IReadOnlyList<string> channels);

        /// <summary>
        /// Stops watching <see cref="ITelemetrySink"/> for the given <paramref name="device"/> and <paramref name="packageId"/>.
        /// </summary>
        void StopWatching(IDevice device, string packageId);
    }
}

