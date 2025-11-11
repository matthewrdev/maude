using SkiaSharp;

namespace Maude;

public static class MaudeChartRenderer
{
    public static void Render(SKCanvas canvas,
                              SKImageInfo info, 
                              IMaudeDataSink dataSink,
                              MaudeRenderOptions renderOptions)
    {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (dataSink == null) throw new ArgumentNullException(nameof(dataSink));
        
        
    }
}