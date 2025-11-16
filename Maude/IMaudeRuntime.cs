namespace Maude;

/// <summary>
/// Contract for controlling the Maude runtime: manage lifecycle, presentation surfaces, and record metrics/events.
/// </summary>
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
    /// Returns true when the slide-in sheet UI is currently presented.
    /// </summary>
    bool IsSheetPresented { get; }
    
    /// <summary>
    /// Should the slide sheet or overlay be allowed to be presented?
    /// </summary>
    bool IsPresentationEnabled { get;  }
    
    /// <summary>
    /// Returns true when the chart overlay is currently presented.
    /// </summary>
    bool IsOverlayPresented { get; }

    /// <summary>
    /// Raised after activation finishes and sampling begins.
    /// </summary>
    event EventHandler OnActivated;
    
    /// <summary>
    /// Raised after deactivation finishes and sampling stops.
    /// </summary>
    event EventHandler OnDeactivated;

    /// <summary>
    /// Starts memory sampling and raises <see cref="OnActivated"/> when complete.
    /// </summary>
    void Activate();
    
    /// <summary>
    /// Stops memory sampling and raises <see cref="OnDeactivated"/> when complete.
    /// </summary>
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
    /// Captures a new event using the given <paramref name="label"/> against the <see cref="MaudeConstants.ReservedChannels.ChannelNotSpecified_Id"/> channel using the default icon.
    /// </summary>
    public void Event(string label);

    /// <summary>
    /// Captures a new event using the given <paramref name="label"/> against the <see cref="MaudeConstants.ReservedChannels.ChannelNotSpecified_Id"/> channel using the provided icon.
    /// </summary>
    public void Event(string label, string icon);
    
    /// <summary>
    /// Captures a new event using the given <paramref name="label"/> against the <see cref="MaudeConstants.ReservedChannels.ChannelNotSpecified_Id"/> channel using the provided icon with the additional <paramref name="details"/>.
    /// </summary>
    public void Event(string label, string icon, string details);
    
    /// <summary>
    /// Captures a new event using the given <paramref name="label"/> against the <paramref name="channel"/> using the default icon.
    /// <para/>
    /// The provided <paramref name="channel"/> <b>must</b> be a built-in channel or predefined during your setup of Maude via <see cref="MaudeRuntime.Initialize"/>.
    /// <para/>
    /// Event's recorded against unknown channels will be discarded.
    /// </summary>
    void Event(string label, byte channel);
    
    /// <summary>
    /// Captures a new event using the given <paramref name="label"/> against the <paramref name="channel"/> using the provided icon.
    /// <para/>
    /// The provided <paramref name="channel"/> <b>must</b> be a built-in channel or predefined during your setup of Maude via <see cref="MaudeRuntime.Initialize"/>.
    /// <para/>
    /// Event's recorded against unknown channels will be discarded.
    /// </summary>
    void Event(string label, string icon, byte channel);
        
    /// <summary>
    /// Captures a new event using the given <paramref name="label"/> against the <paramref name="channel"/> using the provided icon with the additional <paramref name="details"/>.
    /// <para/>
    /// The provided <paramref name="channel"/> <b>must</b> be a built-in channel or predefined during your setup of Maude via <see cref="MaudeRuntime.Initialize"/>.
    /// <para/>
    /// Event's recorded against unknown channels will be discarded.
    /// </summary>
    void Event(string label, string icon, byte channel, string details);
    
    /// <summary>
    /// Clears the backing data sink, removing all recorded metrics and events.
    /// </summary>
    void Clear();
}
