#if IOS
using System;
using System.Linq;
using System.Threading;
using CoreGraphics;
using Foundation;
using SkiaSharp.Views.iOS;
using UIKit;
using System.Collections.Generic;
using SkiaSharp;

namespace Maude;

internal sealed class IosNativePresentationService : IMaudePresentationService
{
    private readonly MaudeOptions options;
    private readonly IMaudeDataSink dataSink;
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private UIViewController? sheetController;
    private UIView? overlayView;
    private MaudeNativeChartViewIos? overlayChart;

    public IosNativePresentationService(MaudeOptions options, IMaudeDataSink dataSink)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.dataSink = dataSink ?? throw new ArgumentNullException(nameof(dataSink));
    }

    public bool IsPresentationEnabled => true;
    public bool IsSheetPresented => sheetController?.PresentedViewController != null || (sheetController?.View?.Window != null);
    public bool IsOverlayPresented => overlayView?.Hidden == false;

    public void PresentSheet()
    {
        var window = PlatformContext.CurrentWindowProvider?.Invoke();
        var root = window?.RootViewController;
        if (root == null)
        {
            MaudeLogger.Error("PresentSheet failed: no UIWindow provided in PlatformContext.");
            return;
        }

        UIApplication.SharedApplication.InvokeOnMainThread(async () =>
        {
            await semaphore.WaitAsync();
            try
            {
                DismissSheetInternal();

                var chart = new MaudeNativeChartViewIos(new CGRect(0, 0, window.Bounds.Width, 220))
                {
                    DataSink = dataSink,
                    RenderMode = MaudeChartRenderMode.Inline,
                    WindowDuration = TimeSpan.FromSeconds(60)
                };

                var table = new UITableView(new CGRect(0, 220, window.Bounds.Width, window.Bounds.Height - 220))
                {
                    SeparatorStyle = UITableViewCellSeparatorStyle.SingleLine,
                    Source = new MaudeEventsTableSource(dataSink)
                };

                var stack = new UIStackView(new UIView[] { chart, table })
                {
                    Axis = UILayoutConstraintAxis.Vertical,
                    Distribution = UIStackViewDistribution.Fill,
                    Alignment = UIStackViewAlignment.Fill
                };

                sheetController = new UIViewController();
                sheetController.View = new UIView(window.Bounds)
                {
                    BackgroundColor = UIColor.SystemBackground
                };
                stack.Frame = sheetController.View.Bounds;
                sheetController.View.AddSubview(stack);

                sheetController.ModalPresentationStyle = UIModalPresentationStyle.PageSheet;

                root.PresentViewController(sheetController, true, null);
            }
            finally
            {
                semaphore.Release();
            }
        });
    }

    public void DismissSheet()
    {
        UIApplication.SharedApplication.InvokeOnMainThread(async () =>
        {
            await semaphore.WaitAsync();
            try
            {
                DismissSheetInternal();
            }
            finally
            {
                semaphore.Release();
            }
        });
    }

    public void PresentOverlay(MaudeOverlayPosition position)
    {
        var window = PlatformContext.CurrentWindowProvider?.Invoke();
        if (window == null)
        {
            MaudeLogger.Error("PresentOverlay failed: no UIWindow provided in PlatformContext.");
            return;
        }

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            if (overlayView == null)
            {
                overlayView = new UIView(window.Bounds)
                {
                    BackgroundColor = UIColor.Clear,
                    UserInteractionEnabled = false,
                    AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
                };

                overlayChart = new MaudeNativeChartViewIos(new CGRect(0, 0, 220, 130))
                {
                    DataSink = dataSink,
                    RenderMode = MaudeChartRenderMode.Overlay,
                    WindowDuration = TimeSpan.FromMinutes(1)
                };

                overlayView.AddSubview(overlayChart);
                window.AddSubview(overlayView);
            }

            PositionOverlay(window, position);
            overlayView.Hidden = false;
        });
    }

    public void DismissOverlay()
    {
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            if (overlayView != null)
            {
                overlayView.Hidden = true;
                overlayChart?.Detach();
            }
        });
    }

    private void PositionOverlay(UIWindow window, MaudeOverlayPosition position)
    {
        if (overlayView == null || overlayChart == null)
        {
            return;
        }

        var margin = 12;
        var frame = overlayChart.Frame;
        switch (position)
        {
            case MaudeOverlayPosition.TopLeft:
                frame.X = margin;
                frame.Y = margin;
                break;
            case MaudeOverlayPosition.TopRight:
                frame.X = window.Bounds.Width - frame.Width - margin;
                frame.Y = margin;
                break;
            case MaudeOverlayPosition.BottomLeft:
                frame.X = margin;
                frame.Y = window.Bounds.Height - frame.Height - margin;
                break;
            default:
                frame.X = window.Bounds.Width - frame.Width - margin;
                frame.Y = window.Bounds.Height - frame.Height - margin;
                break;
        }

        overlayChart.Frame = frame;
    }

    private void DismissSheetInternal()
    {
        if (sheetController == null)
        {
            return;
        }

        try
        {
            sheetController.DismissViewController(true, null);
        }
        catch { }
        finally
        {
            sheetController = null;
        }
    }

    public void Dispose()
    {
        DismissSheetInternal();
        overlayChart?.Detach();
        overlayView?.RemoveFromSuperview();
        overlayView = null;
    }
}

internal sealed class MaudeNativeChartViewIos : SKCanvasView
{
    private IMaudeDataSink? dataSink;
    private NSTimer? timer;
    private float? probeRatio;
    private SKRect? chartBounds;

    public MaudeNativeChartViewIos(CGRect frame) : base(frame)
    {
        PaintSurface += OnPaintSurface;
        var recognizer = new UITapGestureRecognizer(OnTapped);
        AddGestureRecognizer(recognizer);
        timer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(500), _ => SetNeedsDisplay());
    }

    public TimeSpan WindowDuration { get; set; } = TimeSpan.FromMinutes(1);
    public MaudeChartRenderMode RenderMode { get; set; } = MaudeChartRenderMode.Inline;

    public IMaudeDataSink? DataSink
    {
        get => dataSink;
        set
        {
            if (dataSink != null)
            {
                dataSink.OnMetricsUpdated -= HandleMetricsUpdated;
                dataSink.OnEventsUpdated -= HandleEventsUpdated;
            }

            dataSink = value;

            if (dataSink != null)
            {
                dataSink.OnMetricsUpdated += HandleMetricsUpdated;
                dataSink.OnEventsUpdated += HandleEventsUpdated;
            }

            SetNeedsDisplay();
        }
    }

    private void HandleMetricsUpdated(object? sender, MaudeMetricsUpdatedEventArgs e) => SetNeedsDisplay();
    private void HandleEventsUpdated(object? sender, MaudeEventsUpdatedEventArgs e) => SetNeedsDisplay();

    private void OnPaintSurface(object? sender, SkiaSharp.Views.iOS.SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var sink = dataSink;
        if (sink == null)
        {
            canvas.Clear();
            return;
        }

        var channels = sink.Channels?.Select(c => c.Id).ToArray() ?? Array.Empty<byte>();
        var now = DateTime.UtcNow;
        if (WindowDuration <= TimeSpan.Zero)
        {
            WindowDuration = TimeSpan.FromSeconds(30);
        }

        var renderOptions = new MaudeRenderOptions()
        {
            Channels = channels,
            FromUtc = now - WindowDuration,
            ToUtc = now,
            CurrentUtc = now,
            Mode = RenderMode,
            ProbePosition = RenderMode == MaudeChartRenderMode.Inline ? probeRatio : null,
            EventRenderingBehaviour = MaudeRuntime.EventRenderingBehaviour
        };

        var renderResult = MaudeChartRenderer.Render(canvas, e.Info, sink, renderOptions);

        if (renderResult.HasChartArea && renderResult.ChartBounds.Width > 0 && renderResult.ChartBounds.Height > 0)
        {
            chartBounds = renderResult.ChartBounds;
        }
        else
        {
            chartBounds = null;
        }
    }

    private void OnTapped()
    {
        probeRatio = null;
        SetNeedsDisplay();
    }

    public void Detach()
    {
        if (timer != null)
        {
            timer.Invalidate();
            timer.Dispose();
            timer = null;
        }

        if (dataSink != null)
        {
            dataSink.OnMetricsUpdated -= HandleMetricsUpdated;
            dataSink.OnEventsUpdated -= HandleEventsUpdated;
            dataSink = null;
        }

        PaintSurface -= OnPaintSurface;
    }
}

internal sealed class MaudeEventsTableSource : UITableViewSource
{
    private readonly IMaudeDataSink sink;
    private List<MaudeEventDisplay> cache = new();

    public MaudeEventsTableSource(IMaudeDataSink sink)
    {
        this.sink = sink;
        sink.OnEventsUpdated += (_, _) => Refresh();
        Refresh();
    }

    private void Refresh()
    {
        var channelLookup = sink.Channels?.ToDictionary(c => c.Id) ?? new Dictionary<byte, MaudeChannel>();
        cache = sink.Events
                    .OrderByDescending(e => e.CapturedAtUtc)
                    .Take(50)
                    .Select(e => new MaudeEventDisplay
                    {
                        Label = e.Label,
                        Symbol = MaudeEventLegend.GetSymbol(e.Type),
                        ChannelColor = channelLookup.TryGetValue(e.Channel, out var channel) ? channel.Color : new Color(0, 0, 0),
                        Details = e.Details,
                        HasDetails = !string.IsNullOrWhiteSpace(e.Details),
                        Timestamp = e.CapturedAtUtc.ToLocalTime().ToString("HH:mm:ss")
                    })
                    .ToList();
    }

    public override nint RowsInSection(UITableView tableView, nint section) => cache.Count;

    public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
    {
        var cell = tableView.DequeueReusableCell("maude-event") ?? new UITableViewCell(UITableViewCellStyle.Subtitle, "maude-event");
        if (indexPath.Row < cache.Count)
        {
            var display = cache[indexPath.Row];
            cell.TextLabel.Text = $"{display.Symbol} {display.Label}";
            cell.DetailTextLabel.Text = display.HasDetails ? $"{display.Timestamp} • {display.Details}" : display.Timestamp;
        }
        return cell;
    }
}
#endif
