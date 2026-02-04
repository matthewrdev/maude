namespace Maude;

/// <summary>
/// Minimal color value type to avoid external UI dependencies.
/// Stored as 0-255 components with helper accessors.
/// </summary>
public readonly struct Color
{
    public Color(byte red, byte green, byte blue, float alpha = 1f)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = (byte)Math.Clamp(alpha * 255f, 0f, 255f);
    }

    public byte Red { get; }
    public byte Green { get; }
    public byte Blue { get; }
    public byte Alpha { get; }

    public float RedNormalized => Red / 255f;
    public float GreenNormalized => Green / 255f;
    public float BlueNormalized => Blue / 255f;
    public float AlphaNormalized => Alpha / 255f;

    public Color WithAlpha(float alpha)
    {
        return new Color(Red, Green, Blue, alpha);
    }
}

public static class Colors
{
    public static readonly Color Transparent = new Color(0, 0, 0, 0f);
    public static readonly Color White = new Color(255, 255, 255);
    public static readonly Color Black = new Color(0, 0, 0);
    public static readonly Color Gray = new Color(128, 128, 128);
    public static readonly Color WhiteSmoke = new Color(245, 245, 245);
    public static readonly Color Purple = new Color(123, 97, 255);
}
