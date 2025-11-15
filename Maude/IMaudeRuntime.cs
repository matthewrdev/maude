namespace Maude;

public interface IMaudeRuntime
{
    /// <summary>
    /// The backing data sink being used by Maude.
    /// </summary>
    IMaudeDataSink DataSink { get; }
    
    bool IsActive { get; }
    
    bool IsPresented { get; }
    
    bool IsPresentationEnabled { get;  }
    
    bool IsChartOverlayPresented { get; }

    event EventHandler OnActivated;
    
    event EventHandler OnDeactivated;

    void Activate();
    
    void Deactivate();
    
    void Present();
    
    void Dismiss();

    void PresentChartOverlay(MaudeOverlayPosition position = MaudeOverlayPosition.TopRight);
    
    void DismissChartOverlay();
    
    // hide/show
    // Add event
    void Metric(long value, byte channel);
    
    void Event(string label, byte channel);
    
    void Event(string label, string icon, byte channel);
    
    void Clear();
}
