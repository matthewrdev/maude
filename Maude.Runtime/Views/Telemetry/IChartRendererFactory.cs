using System;
using Ansight.Adb.Telemetry;

namespace Maude.Runtime.Views.Telemetry
{
    public interface IChartRendererFactory
    {
        IChartRenderer Create(ITelemetryChannel channel);
    }
}

