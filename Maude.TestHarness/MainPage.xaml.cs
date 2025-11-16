namespace Maude.TestHarness;

public partial class MainPage : ContentPage
{
    private readonly List<byte[]> spikes = new();
    private readonly List<object> nativeAllocations = new();
    private readonly Stack<Action> releaseActions = new();
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
        MaudeRuntime.PresentSheet();
        UpdateRuntimeStatus();
    }

    private void OnPresentChartOverlayClicked(object? sender, EventArgs e)
    {
        if (MaudeRuntime.IsChartOverlayPresented)
        {
            MaudeRuntime.DismissOverlay();
        }
        else
        {
            MaudeRuntime.PresentOverlay(MaudeOverlayPosition.TopRight);
        }

        UpdateRuntimeStatus();
    }

    private void OnDismissClicked(object? sender, EventArgs e)
    {
#if ANDROID || IOS
        MaudeRuntime.DismissOverlay();
#endif
        
        MaudeRuntime.DismissSheet();
        UpdateRuntimeStatus();
    }

    private void PresentOverlay(MaudeOverlayPosition position)
    {
        MaudeRuntime.PresentOverlay(position);
        UpdateRuntimeStatus();
    }

    private void OnOverlayTopLeftClicked(object? sender, EventArgs e) => PresentOverlay(MaudeOverlayPosition.TopLeft);

    private void OnOverlayTopRightClicked(object? sender, EventArgs e) => PresentOverlay(MaudeOverlayPosition.TopRight);

    private void OnOverlayBottomLeftClicked(object? sender, EventArgs e) => PresentOverlay(MaudeOverlayPosition.BottomLeft);

    private void OnOverlayBottomRightClicked(object? sender, EventArgs e) => PresentOverlay(MaudeOverlayPosition.BottomRight);


    private void OnLowSpikeClicked(object? sender, EventArgs e) => RunClrMemorySpike("Low", 4).SafeFireAndForget();

    private void OnMediumSpikeClicked(object? sender, EventArgs e) => RunClrMemorySpike("Medium", 16).SafeFireAndForget();

    private void OnHighSpikeClicked(object? sender, EventArgs e) => RunClrMemorySpike("High", 48).SafeFireAndForget();

    private  void OnExtremeSpikeClicked(object? sender, EventArgs e) => RunClrMemorySpike("Extreme", 96).SafeFireAndForget();

    private  void OnLowNativeClicked(object? sender, EventArgs e) => RunNativeSpike("Native Low", 500).SafeFireAndForget();

    private  void OnMediumNativeClicked(object? sender, EventArgs e) => RunNativeSpike("Native Medium", 2000).SafeFireAndForget();

    private  void OnHighNativeClicked(object? sender, EventArgs e) => RunNativeSpike("Native High", 4000).SafeFireAndForget();

    private  void OnExtremeNativeClicked(object? sender, EventArgs e) => RunNativeSpike("Native Extreme", 12000).SafeFireAndForget();

    private Task RunClrMemorySpike(string label, int sizeMb)
    {
        try
        {
            var buffer = new byte[sizeMb * 1024 * 1024];
            lock (spikeLock)
            {
                spikes.Add(buffer);
            }

            MaudeRuntime.Event($"Memory spike ({label})", CustomMaudeConfiguration.CustomEventChannelId);

            releaseActions.Push(() =>
            {
                lock (spikeLock)
                {
                    spikes.Remove(buffer);
                }
            });
        }
        catch (OutOfMemoryException)
        {
            MaudeRuntime.Event($"Failed spike ({label})", MaudeConstants.ReservedChannels.ChannelNotSpecified_Id);
        }
        finally
        {
            UpdateRuntimeStatus();
        }

        return Task.CompletedTask;
    }

    private async Task RunNativeSpike(string label, int count)
    {
        try
        {
#if ANDROID
            var list = new List<Java.Lang.Object>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new Java.Lang.String($"maude-{i}-{DateTime.UtcNow.Ticks}"));
            }
            MaudeRuntime.Event($"{label} (Java objects)", CustomMaudeConfiguration.CustomEventChannelId);
            nativeAllocations.Add(list);
            releaseActions.Push(() =>
            {
                list.Clear();
                nativeAllocations.Remove(list);
            });
#elif IOS || MACCATALYST
            var list = new List<Foundation.NSObject>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new Foundation.NSString($"maude-{i}-{DateTime.UtcNow.Ticks}"));
            }
            MaudeRuntime.Event($"{label} (NSObjects)", CustomMaudeConfiguration.CustomEventChannelId);
            nativeAllocations.Add(list);
            releaseActions.Push(() =>
            {
                list.Clear();
                nativeAllocations.Remove(list);
            });
#else
            await Task.CompletedTask;
#endif
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            UpdateRuntimeStatus();
        }
    }

    private void OnReleaseAllocationClicked(object? sender, EventArgs e)
    {
        if (releaseActions.Count == 0)
        {
            return;
        }

        var action = releaseActions.Pop();
        action();

        MaudeRuntime.Event("Released allocation", CustomMaudeConfiguration.CustomEventChannelId);
        UpdateRuntimeStatus();
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

    private async void OnPushNavigationPageClicked(object? sender, EventArgs e)
    {
        if (Navigation == null)
        {
            return;
        }

        await Navigation.PushAsync(new NavigationTestPage());
    }

    private async void OnPushModalPageClicked(object? sender, EventArgs e)
    {
        if (Navigation == null)
        {
            return;
        }

        await Navigation.PushModalAsync(new ModalTestPage());
    }
}
