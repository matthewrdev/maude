using System;

namespace Maude.Runtime.Telemetry.Sampling;

public interface ITelemetryStream
{
	Guid Id { get; }

	string DeviceId { get; }

	string PackageId { get; }

	string Channel { get; }

	TimeSpan DeviceTimeOffset { get; }

	bool IsRunning { get; }

	event EventHandler<TelemetrySamplesEventArgs> OnNewTelemetrySamples;
}