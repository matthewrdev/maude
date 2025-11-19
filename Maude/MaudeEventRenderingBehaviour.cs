namespace Maude;

/// <summary>
/// Controls how annotated Maude events are visualised on the chart.
/// </summary>
public enum MaudeEventRenderingBehaviour : byte
{
    /// <summary>
    /// Draw both the event icon and its label near the metric line.
    /// </summary>
    LabelsAndIcons = 0,
    
    /// <summary>
    /// Draw event icons only.
    /// </summary>
    IconsOnly = 1,
    
    /// <summary>
    /// Do not render events or their vertical guide lines.
    /// </summary>
    None = 2
}
