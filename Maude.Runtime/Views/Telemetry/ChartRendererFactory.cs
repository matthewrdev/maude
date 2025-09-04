using System;
using System.ComponentModel.Composition;
using Ansight.Adb.Telemetry;
using Maude.Runtime.Views.Telemetry;
using Maude.Runtime.Views.Telemetry.Renderers;

namespace Maude.Runtime.Views.Telemetry
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IChartRendererFactory))]
    public class ChartRendererFactory : IChartRendererFactory
    {
        public IChartRenderer Create(ITelemetryChannel channel)
        {
            if (channel is null)
            {
                throw new ArgumentNullException(nameof(channel));
            }

            switch (channel.Name)
            {
                case TelemetryKinds.Memory:
                    return new MemoryChartRenderer(ChartRenderingOptions.Memory);
                case TelemetryKinds.CPU:
                    return new CPUChartRenderer(ChartRenderingOptions.CPU);
                case TelemetryKinds.Graphics:
                    return new GraphicsChartRenderer(ChartRenderingOptions.Rendering);
            }

            return new ChartRenderer(ChartRenderingOptions.Default);
        }
    }
}