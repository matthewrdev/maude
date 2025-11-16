namespace Maude;

/// <summary>
/// Controls whether the chart renders inline or in the overlay layout.
/// </summary>
public enum MaudeChartRenderMode
{
    /// <summary>
    /// Render inside the slide-in sheet.
    /// </summary>
    Inline = 0,
    
    /// <summary>
    /// Render inside the window overlay.
    /// </summary>
    Overlay = 1
}
