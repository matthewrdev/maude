using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Ansight.IOC;

namespace Maude.Runtime.Telemetry.Sampling
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ITelemetryStreamFactoryRepository))]
    public class TelemetryStreamFactoryRepository : PartRepository<ITelemetryStreamFactory>, ITelemetryStreamFactoryRepository
    {
        [ImportingConstructor]
        public TelemetryStreamFactoryRepository(Lazy<IPartResolver> partResolver) : base(partResolver)
        {
        }

        public IReadOnlyList<ITelemetryStreamFactory> TelemetryStreamFactories => Parts;

        public IReadOnlyList<string> Channels => TelemetryStreamFactories.Select(ts => ts.Channel).ToList();

        public ITelemetryStreamFactory GetStreamFactoryForChannel(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or whitespace.", nameof(channel));
            }

            return TelemetryStreamFactories.FirstOrDefault(ts => ts.Channel == channel);
        }
    }
}