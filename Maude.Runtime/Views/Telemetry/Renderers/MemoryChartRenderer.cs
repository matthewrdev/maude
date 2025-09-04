using Ansight.Utilities;

namespace Maude.Runtime.Views.Telemetry.Renderers
{
    public class MemoryChartRenderer : ChartRenderer, IChartRenderer
    {
        // Divided into 8ths
        // 
        // 1:  Memory (Chart Title)
        // 2:  ╍ Total ▪️ Java ▪️ Stack  (Legend view)
        // 3:  XXX KB  | -  -  -  -  -  -  -  -  -
        // 4:          |   ----------- 
        // 5:  XXX KB  | -/ -  -  -  -\ -  -  -  -
        // 6:          | /             \----------
        // 7:  XXX KB  |-----------|----------|---
        // 8:         (Time)     (Time)     (Time)
        //   ^--------^^------------------------->
        //      100px   Full width to end with buffer of 16 PX on right hand side

        // Filled area for all non total groups.
        // Dash white line for the 

        public MemoryChartRenderer(IChartRenderingOptions options)
            : base(options)
        {
        }

        protected override string GetLabelledValue(double value, string axisLabel)
        {
            var bytes = value * 1024;

            return SizeHelper.GetFormattedSize((long)bytes, Environment.NewLine);
        }
    }
}

