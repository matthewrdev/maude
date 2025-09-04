namespace Maude.Runtime.Views.Telemetry.Renderers
{
    public class GraphicsChartRenderer : ChartRenderer, IChartRenderer
    {
        public GraphicsChartRenderer(IChartRenderingOptions options)
            : base(options)
        {
        }

        protected override string GetLabelledValue(double value, string axisLabel)
        {
            value = Math.Floor(value);
            return $"{value}";
        }
    }
}

