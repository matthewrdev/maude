using SkiaSharp;

namespace Maude;

public readonly struct MaudeRenderResult
{
    public MaudeRenderResult(SKRect chartBounds, bool hasChartArea)
    {
        ChartBounds = chartBounds;
        HasChartArea = hasChartArea;
    }

    public SKRect ChartBounds { get; }

    public bool HasChartArea { get; }

    public static MaudeRenderResult Empty => new MaudeRenderResult(SKRect.Empty, false);
}
