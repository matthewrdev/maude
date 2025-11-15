using System.Collections.Generic;
using System.Threading.Tasks;
using Maude;

namespace Maude.TestHarness;

public partial class MainPage : ContentPage
{
    private readonly List<byte[]> spikes = new();
    private readonly object spikeLock = new();
    private readonly Random random = new();

    public MainPage()
    {
        InitializeComponent();
        UpdateRuntimeStatus();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateRuntimeStatus();
    }

    private void UpdateRuntimeStatus()
    {
        var isActive = MaudeRuntime.IsActive();
        var isPresented = MaudeRuntime.IsPresented;
        StatusLabel.Text = $"Runtime {(isActive ? "active" : "inactive")} • {(isPresented ? "presented" : "hidden")}";
    }

    private void OnActivateClicked(object? sender, EventArgs e)
    {
        MaudeRuntime.Activate();
        UpdateRuntimeStatus();
    }

    private void OnDeactivateClicked(object? sender, EventArgs e)
    {
        MaudeRuntime.Deactivate();
        UpdateRuntimeStatus();
    }

    private void OnTriggerGcClicked(object? sender, EventArgs e)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        MaudeRuntime.Event("Manual GC", CustomMaudeConfiguration.CustomEventChannelId);
        UpdateRuntimeStatus();
    }

    private void OnPresentSheetClicked(object? sender, EventArgs e)
    {
        MaudeRuntime.Present();
        UpdateRuntimeStatus();
    }

    private void OnPresentChartOverlayClicked(object? sender, EventArgs e)
    {
        MaudeRuntime.PresentChartOverlay(MaudeOverlayPosition.TopRight);
        UpdateRuntimeStatus();
    }

    private void OnDismissClicked(object? sender, EventArgs e)
    {
#if ANDROID || IOS
        MaudeRuntime.DismissChartOverlay();
#endif
        
        MaudeRuntime.Dismiss();
        UpdateRuntimeStatus();
    }


    private async void OnLowSpikeClicked(object? sender, EventArgs e) => await RunMemorySpike("Low", 4);

    private async void OnMediumSpikeClicked(object? sender, EventArgs e) => await RunMemorySpike("Medium", 16);

    private async void OnHighSpikeClicked(object? sender, EventArgs e) => await RunMemorySpike("High", 48);

    private async void OnExtremeSpikeClicked(object? sender, EventArgs e) => await RunMemorySpike("Extreme", 96);

    private async Task RunMemorySpike(string label, int sizeMb)
    {
        try
        {
            var buffer = new byte[sizeMb * 1024 * 1024];
            lock (spikeLock)
            {
                spikes.Add(buffer);
            }

            MaudeRuntime.Event($"Memory spike ({label})", CustomMaudeConfiguration.CustomEventChannelId);

            await Task.Delay(TimeSpan.FromSeconds(5));

            lock (spikeLock)
            {
                spikes.Remove(buffer);
            }
        }
        catch (OutOfMemoryException)
        {
            MaudeRuntime.Event($"Failed spike ({label})", MaudeConstants.ReservedChannels.ChannelNotSpecified_Id);
        }
        finally
        {
            UpdateRuntimeStatus();
        }
    }

    private void OnRecordCustomMetricClicked(object? sender, EventArgs e)
    {
        var value = random.Next(8, 200) * 1024L * 1024L;
        MaudeRuntime.Metric(value, CustomMaudeConfiguration.CustomMetricChannelId);
        UpdateRuntimeStatus();
    }

    private void OnRecordCustomEventClicked(object? sender, EventArgs e)
    {
        MaudeRuntime.Event($"Custom event @ {DateTime.Now:HH:mm:ss}", CustomMaudeConfiguration.CustomEventChannelId);
        UpdateRuntimeStatus();
    }

    private void OnRecordDetachedEventClicked(object? sender, EventArgs e)
    {
        MaudeRuntime.Event("Detached channel event", MaudeConstants.ReservedChannels.ChannelNotSpecified_Id);
        UpdateRuntimeStatus();
    }

    private void OnClearDataClicked(object? sender, EventArgs e)
    {
        MaudeRuntime.Clear();
        UpdateRuntimeStatus();
    }

    private void OnCloseOverlayClicked(object? sender, EventArgs e)
    {
        UpdateRuntimeStatus();
    }
}
