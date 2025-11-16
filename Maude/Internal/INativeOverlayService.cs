namespace Maude;

/// <summary>
/// Platform-specific overlay presenter that hosts the Maude chart inside native UI chrome.
/// </summary>
internal interface INativeOverlayService
{
    /// <summary>
    /// True when the overlay is currently visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Shows the overlay using the provided <paramref name="dataSink"/> and anchor <paramref name="position"/>.
    /// </summary>
    void Show(IMaudeDataSink dataSink, MaudeOverlayPosition position);

    /// <summary>
    /// Hides the overlay and releases any platform resources.
    /// </summary>
    void Hide();
}
