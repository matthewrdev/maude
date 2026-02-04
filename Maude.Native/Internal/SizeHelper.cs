namespace Maude;

/// <summary>
/// A helper class for pretty formatting a size in bytes
/// </summary>
internal static class SizeHelper
{
    /// <summary>
    /// The suffixes for various file sizes.
    /// </summary>
    private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

    /// <summary>
    /// Using the <paramref name="lengthInBytes"/>, gets the formatted file size string.
    /// </summary>
    public static string GetFormattedSize(long lengthInBytes,
        string separator = "",
        bool includeBrackets = false)
    {
        if (lengthInBytes < 0) { return "-" + GetFormattedSize(-lengthInBytes); }

        var i = 0;
        var dValue = (decimal)lengthInBytes;
        while (Math.Round(dValue / 1024) >= 1)
        {
            dValue /= 1024;
            i++;
        }

        if (includeBrackets)
        {
            return $"{dValue:0.#}{separator}({SizeSuffixes[i]})";
        }

        return $"{dValue:0.#}{separator}{SizeSuffixes[i]}";
    }
}