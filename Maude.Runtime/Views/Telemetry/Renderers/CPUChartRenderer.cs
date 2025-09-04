using System;
using Ansight.Adb.Telemetry;
using Ansight.TimeZones;
using Ansight.Utilities;
using SkiaSharp;

namespace Maude.Runtime.Views.Telemetry.Renderers
{
    public class CPUChartRenderer : ChartRenderer, IChartRenderer
    {
        public CPUChartRenderer(IChartRenderingOptions options)
            : base(options)
        {
        }

        protected override string GetLabelledValue(double value, string axisLabel)
        {
            return value.ToString("0.00") + "%";
        }
    }
}

