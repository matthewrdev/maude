#if ANDROID || IOS
namespace Maude;

internal sealed class MaudeChartWindowOverlay : WindowOverlay, IDisposable
{
    private readonly IMaudeDataSink dataSink;
    private readonly MaudeChartOverlayElement chartElement;

    public MaudeChartWindowOverlay(Window window, 
                                   IMaudeDataSink sink, 
                                   MaudeOverlayPosition position) : base(window)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(sink);

        dataSink = sink;
        chartElement = new MaudeChartOverlayElement(sink, position);

        AddWindowElement(chartElement);
        EnableDrawableTouchHandling = false;

        dataSink.OnMetricsUpdated += HandleSinkUpdated;
        dataSink.OnEventsUpdated += HandleSinkUpdated;
    }

    public void UpdatePosition(MaudeOverlayPosition position)
    {
        chartElement.Position = position;
        InvalidateOnMainThread();
    }

    public void RequestRedraw() => InvalidateOnMainThread();

    private void HandleSinkUpdated(object? sender, EventArgs e)
    {
        InvalidateOnMainThread();
    }

    private void InvalidateOnMainThread()
    {
        if (MainThread.IsMainThread)
        {
            Invalidate();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(Invalidate);
        }
    }

    public void Dispose()
    {
        dataSink.OnMetricsUpdated -= HandleSinkUpdated;
        dataSink.OnEventsUpdated -= HandleSinkUpdated;

        RemoveWindowElement(chartElement);
        chartElement.Dispose();
    }
}
#endif