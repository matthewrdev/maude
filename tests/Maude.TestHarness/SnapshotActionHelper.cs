using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;

namespace Maude.TestHarness;

internal static class SnapshotActionHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    public static async Task CopySnapshotToClipboardAsync(MaudeSnapshot snapshot)
    {
        var json = JsonSerializer.Serialize(snapshot, SerializerOptions);
        await Clipboard.Default.SetTextAsync(json);
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = Application.Current?.MainPage;
            if (page != null)
            {
                await page.DisplayAlert("Snapshot copied", "Snapshot JSON copied to clipboard.", "OK");
            }
        });
    }
}
