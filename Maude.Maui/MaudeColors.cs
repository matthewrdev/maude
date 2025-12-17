using MauiColor = Microsoft.Maui.Graphics.Color;

namespace Maude;

/// <summary>
/// Helpers for converting Maude's internal color struct to MAUI colors.
/// </summary>
public static class MaudeColors
{
    public static MauiColor BrandColor => ToMauiColor(MaudeConstants.MaudeBrandColor);
    public static MauiColor BrandColorFaded => ToMauiColor(MaudeConstants.MaudeBrandColor_Faded);

    public static MauiColor ToMauiColor(Color color) =>
        new MauiColor(color.RedNormalized, color.GreenNormalized, color.BlueNormalized, color.AlphaNormalized);
}
