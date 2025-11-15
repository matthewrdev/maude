namespace Maude;

public interface IMaudeRuntime
{
    /// <summary>
    /// The backing data sink being used by Maude.
    /// </summary>
    IMaudeDataSink DataSink { get; }
    
    /// <summary>
    /// If Maude is currently performing memory tracking.
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// 
    /// </summary>
    bool IsSheetPresented { get; }
    
    /// <summary>
    /// Should the slide sheet or overlay be allowed to be presented?
    /// </summary>
    bool IsPresentationEnabled { get;  }
    
    
    bool IsOverlayPresented { get; }

    /// <summary>
    /// Occurs when 
    /// </summary>
    event EventHandler OnActivated;
    
    event EventHandler OnDeactivated;

    
    void Activate();
    
    void Deactivate();
    
    /// <summary>
    /// Opens the charting presentation slide in sheet.
    /// </summary>
    void PresentSheet();
    
    /// <summary>
    /// Closes the charting presentation slide in sheet.
    /// </summary>
    void DismissSheet();

    /// <summary>
    /// Shows the chart overlay on the current window at the given <paramref name="position"/>.
    /// </summary>
    void PresentOverlay(MaudeOverlayPosition position = MaudeOverlayPosition.TopRight);
    
    /// <summary>
    /// Hides and disposes the chart overlay.
    /// </summary>
    void DismissOverlay();
    
    /// <summary>
    /// Enables shake gesture handling if it is configured in <see cref="MaudeOptions"/>.
    /// </summary>
    void EnableShakeGesture();
    
    /// <summary>
    /// Disables shake gesture handling if previously enabled.
    /// </summary>
    void DisableShakeGesture();
    
    /// <summary>
    /// Captures a new metric using the given <paramref name="value"/> against the <paramref name="channel"/>
    /// <para/>
    /// The provided <paramref name="channel"/> <b>must</b> be a built-in channel or predefined during your setup of Maude via <see cref="MaudeRuntime.Initialize"/>.
    /// <para/>
    /// Metrics recorded against unknown channels will be discarded.
    /// </summary>
    void Metric(long value, byte channel);
    
    /// <summary>
    /// Captures a new event using the given <paramref name="label"/> against the <paramref name="channel"/> using the default icon.
    /// <para/>
    /// The provided <paramref name="channel"/> <b>must</b> be a built-in channel or predefined during your setup of Maude via <see cref="MaudeRuntime.Initialize"/>.
    /// <para/>
    /// Event's recorded against unknown channels will be discarded.
    /// </summary>
    void Event(string label, byte channel);
    
    /// <summary>
    /// Captures a new event using the given <paramref name="label"/> against the <paramref name="channel"/> using the given 
    /// <para/>
    /// The provided <paramref name="channel"/> <b>must</b> be a built-in channel or predefined during your setup of Maude via <see cref="MaudeRuntime.Initialize"/>.
    /// <para/>
    /// Event's recorded against unknown channels will be discarded.
    /// </summary>
    void Event(string label, string icon, byte channel);
    
    /// <summary>
    /// Clears the backing data sink, removing all recorded metrics and events.
    /// </summary>
    void Clear();
}
