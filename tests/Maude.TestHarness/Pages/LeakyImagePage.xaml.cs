using SkiaSharp;

namespace Maude.TestHarness;

public partial class ImagePage : ContentPage
{
    private static readonly List<SKBitmap> leakedBitmaps = new();
    private static readonly List<SKImage> leakedImages = new();
    private static readonly Random random = new();

    public ImagePage()
    {
        InitializeComponent();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        StatusLabel.Text = $"Leaked images: {leakedImages.Count}";
    }

    private void OnLeakImageClicked(object? sender, EventArgs e)
    {
        const int width = 4096;
        const int height = 4096; // ~67 MB for RGBA

        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(new SKColor((uint)random.Next()));
        }

        var image = SKImage.FromBitmap(bitmap);

        leakedBitmaps.Add(bitmap);
        leakedImages.Add(image);

        UpdateStatus();
    }
}
