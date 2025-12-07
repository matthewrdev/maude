using Microsoft.Maui.Controls;

namespace Maude;

/// <summary>
/// Composite view that hosts the Maude chart, event list, and controls inside the slide-in sheet.
/// </summary>
public partial class MaudeView : Grid
{
    private readonly IReadOnlyList<TagSelectorOption> windowOptions = new[]
    {
        new TagSelectorOption("20s", TimeSpan.FromSeconds(20)),
        new TagSelectorOption("40s", TimeSpan.FromSeconds(40)),
        new TagSelectorOption("60s", TimeSpan.FromSeconds(60)),
        new TagSelectorOption("90s", TimeSpan.FromSeconds(90)),
        new TagSelectorOption("120s", TimeSpan.FromSeconds(120)),
        new TagSelectorOption("150s", TimeSpan.FromSeconds(150)),
        new TagSelectorOption("180s", TimeSpan.FromSeconds(180)),
        new TagSelectorOption("240s", TimeSpan.FromSeconds(240)),
        new TagSelectorOption("300s", TimeSpan.FromSeconds(300)),
    };

    public MaudeView()
    {
        InitializeComponent();
        
        BindRuntime();
        BindWindowSelector();
        UpdateWindowLabel();
        InitializeSnapshotAction();
    }
    
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        if (args.NewHandler == null)
        {
            UnbindRuntime();
        }
    }
    
    private void OnToggleOverlayTapped(object sender, TappedEventArgs e)
    {
        if (MaudeRuntime.IsChartOverlayPresented)
        {
            MaudeRuntime.DismissOverlay();
        }
        else
        {
            MaudeRuntime.PresentOverlay();
        }
    }

    private void InitializeSnapshotAction()
    {
        var action = MaudeRuntime.MutableInstance.SaveSnapshotAction;
        var isEnabled = action != null;
        snapshotActionButton.IsVisible = isEnabled;

        if (isEnabled && action != null)
        {
            snapshotActionLabel.Text = action.Label;
            if (string.IsNullOrWhiteSpace(action.Label))
            {
                snapshotActionLabel.Text = "COPY";
            }
        }
    }

    private async void OnSaveSnapshotTapped(object? sender, TappedEventArgs e)
    {
        await MaudeRuntime.MutableInstance.ExecuteSaveSnapshotActionAsync();
    }

    private void BindRuntime()
    {
        chartView.DataSink = MaudeRuntime.Instance.DataSink;
        eventsView.DataSink = MaudeRuntime.Instance.DataSink;
    }

    private void BindWindowSelector()
    {
        intervalSelector.Items = windowOptions;
        intervalSelector.SelectedItem = windowOptions.FirstOrDefault(o => o.Duration == TimeSpan.FromSeconds(60)) ?? windowOptions.First();
        intervalSelector.SelectionChanged -= OnIntervalSelectionChanged;
        intervalSelector.SelectionChanged += OnIntervalSelectionChanged;

        chartView.WindowDuration = intervalSelector.SelectedItem?.Duration ?? TimeSpan.FromSeconds(60);
        UpdateWindowLabel();
    }

    public void UnbindRuntime()
    {
        chartView.Detach();
        eventsView.Detach();
        DetachWindowSelector();
    }

    private void UpdateWindowLabel()
    {
        var seconds = Math.Max(1, (int)Math.Round(chartView.WindowDuration.TotalSeconds));
        windowLabel.Text = $"Last {seconds}s";
    }

    private void DetachWindowSelector()
    {
        if (intervalSelector != null)
        {
            intervalSelector.SelectionChanged -= OnIntervalSelectionChanged;
        }
    }

    private void OnIntervalSelectionChanged(object? sender, TagSelectorSelectionChangedEventArgs args)
    {
        if (args?.Selected?.Duration is TimeSpan duration && duration > TimeSpan.Zero)
        {
            chartView.WindowDuration = duration;
            UpdateWindowLabel();
        }
    }
}
