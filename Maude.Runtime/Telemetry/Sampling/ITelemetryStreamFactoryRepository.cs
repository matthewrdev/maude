using System;
using System.Collections.Generic;
using Ansight.IOC;

namespace Maude.Runtime.Telemetry.Sampling
{
    public interface ITelemetryStreamFactoryRepository : IPartRepository<ITelemetryStreamFactory>
    {
        IReadOnlyList<ITelemetryStreamFactory> TelemetryStreamFactories { get; }

        IReadOnlyList<string> Channels { get; }

        ITelemetryStreamFactory GetStreamFactoryForChannel(string channel);
    }
}

