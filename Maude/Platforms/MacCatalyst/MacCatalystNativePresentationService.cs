#if MACCATALYST
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CoreGraphics;
using Foundation;
using SkiaSharp;
using SkiaSharp.Views.iOS;
using UIKit;

namespace Maude;

internal sealed class MacCatalystNativePresentationService : IMaudePresentationService
{
    private readonly MaudeOptions options;
    private readonly IMaudeDataSink dataSink;
    private readonly Func<UIWindow?> windowProvider;
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private UIViewController? sheetController;
    private DismissablePresentationDelegate? sheetPresentationDelegate;
    private UIView? overlayView;
    private MaudeNativeChartViewMacCatalyst? overlayChart;

    public MacCatalystNativePresentationService(MaudeOptions options, IMaudeDataSink dataSink, Func<UIWindow?> windowProvider)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.dataSink = dataSink ?? throw new ArgumentNullException(nameof(dataSink));
        this.windowProvider = windowProvider ?? throw new ArgumentNullException(nameof(windowProvider));
    }

    public bool IsPresentationEnabled => true;
    public bool IsSheetPresented => sheetController?.PresentedViewController != null || (sheetController?.View?.Window != null);
    public bool IsOverlayPresented => overlayView?.Hidden == false;

    public void PresentSheet()
    {
        var window = windowProvider();
        var root = window?.RootViewController;
        if (root == null)
        {
            MaudeLogger.Error("PresentSheet failed: no UIWindow available.");
            return;
        }

        UIApplication.SharedApplication.InvokeOnMainThread(async () =>
        {
            await semaphore.WaitAsync();
            try
            {
                DismissSheetInternal();

                sheetController = new UIViewController();
                sheetController.View = new MaudeSheetView(window.Bounds, dataSink, options);
                sheetController.ModalPresentationStyle = UIModalPresentationStyle.PageSheet;
                sheetController.ModalInPresentation = false;

                var presentationController = sheetController.PresentationController;
                if (presentationController != null)
                {
                    sheetPresentationDelegate = new DismissablePresentationDelegate(() =>
                    {
                        sheetController = null;
                        sheetPresentationDelegate = null;
                    });
                    presentationController.Delegate = sheetPresentationDelegate;
                }

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
        var window = windowProvider();
        if (window == null)
        {
            MaudeLogger.Error("PresentOverlay failed: no UIWindow available.");
            return;
        }

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            EnsureOverlay(window);

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
            }
        });
    }

    private void EnsureOverlay(UIWindow window)
    {
        if (overlayView == null)
        {
            overlayView = new UIView(window.Bounds)
            {
                BackgroundColor = UIColor.Clear,
                UserInteractionEnabled = false,
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
            };

            window.AddSubview(overlayView);
        }

        if (overlayChart == null || overlayChart.DataSink == null || overlayChart.Superview == null)
        {
            overlayChart = new MaudeNativeChartViewMacCatalyst(new CGRect(0, 0, 320, 180))
            {
                DataSink = dataSink,
                RenderMode = MaudeChartRenderMode.Overlay,
                WindowDuration = TimeSpan.FromMinutes(1)
            };

            overlayView.Subviews?.FirstOrDefault()?.RemoveFromSuperview();
            overlayView.AddSubview(overlayChart);
        }
    }

    private void PositionOverlay(UIWindow window, MaudeOverlayPosition position)
    {
        if (overlayView == null || overlayChart == null)
        {
            return;
        }

        var margin = 16;
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
            sheetPresentationDelegate = null;
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

internal sealed class MaudeNativeChartViewMacCatalyst : SKCanvasView
{
    private IMaudeDataSink? dataSink;
    private NSTimer? timer;
    private float? probeRatio;
    private SKRect? chartBounds;

    public MaudeNativeChartViewMacCatalyst(CGRect frame) : base(frame)
    {
        IgnorePixelScaling = true;
        PaintSurface += OnPaintSurface;
        timer = NSTimer.CreateRepeatingScheduledTimer(TimeSpan.FromMilliseconds(500), _ => RequestRedraw());
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

            RequestRedraw();
        }
    }

    private void HandleMetricsUpdated(object? sender, MaudeMetricsUpdatedEventArgs e) => RequestRedraw();
    private void HandleEventsUpdated(object? sender, MaudeEventsUpdatedEventArgs e) => RequestRedraw();

    private void RequestRedraw()
    {
        if (NSThread.IsMain)
        {
            SetNeedsDisplay();
            return;
        }

        UIApplication.SharedApplication.InvokeOnMainThread(SetNeedsDisplay);
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
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

internal sealed class MaudeSheetView : UIView
{
    private readonly UILabel titleLabel;
    private readonly UIButton overlayButton;
    private readonly UIButton closeButton;
    private readonly UIButton? copyButton;
    private readonly MaudeNativeChartViewMacCatalyst chart;
    private readonly UITableView table;

    private static readonly nfloat ChartHeight = new nfloat(220);
    private static readonly nfloat HorizontalPadding = new nfloat(12);
    private static readonly nfloat VerticalPadding = new nfloat(12);
    private static readonly nfloat SectionSpacing = new nfloat(8);
    private static readonly nfloat ButtonHeight = new nfloat(34);

    internal MaudeSheetView(CGRect frame, IMaudeDataSink dataSink, MaudeOptions options) : base(frame)
    {
        BackgroundColor = UIColor.SystemBackground;
        AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

        titleLabel = new UILabel
        {
            Text = "MEMORY OVERVIEW",
            Font = UIFont.BoldSystemFontOfSize(16),
            TextColor = UIColor.Label,
            Lines = 1
        };

        overlayButton = CreatePillButton("OVERLAY");
        overlayButton.TouchUpInside += (_, _) => ToggleOverlay();

        closeButton = CreatePillButton("CLOSE");
        closeButton.TouchUpInside += (_, _) => MaudeRuntime.DismissSheet();

        if (options.SaveSnapshotAction != null)
        {
            var action = options.SaveSnapshotAction;
            var label = string.IsNullOrWhiteSpace(action.Label) ? "COPY" : action.Label;
            copyButton = CreatePillButton(label);
            copyButton.TouchUpInside += async (_, _) => await ExecuteSaveSnapshotAsync(dataSink, action);
            AddSubview(copyButton);
        }

        chart = new MaudeNativeChartViewMacCatalyst(new CGRect(0, 0, frame.Width, ChartHeight))
        {
            DataSink = dataSink,
            RenderMode = MaudeChartRenderMode.Inline,
            WindowDuration = TimeSpan.FromSeconds(60),
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth
        };

        table = new UITableView
        {
            SeparatorStyle = UITableViewCellSeparatorStyle.SingleLine,
            Source = new MaudeEventsTableSource(dataSink),
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
        };

        AddSubview(titleLabel);
        AddSubview(overlayButton);
        AddSubview(closeButton);
        AddSubview(chart);
        AddSubview(table);
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        var safe = SafeAreaInsets;
        var width = Bounds.Width;
        var y = safe.Top + VerticalPadding;
        var availableRight = width - HorizontalPadding;

        var closeSize = closeButton.SizeThatFits(new CGSize(width, ButtonHeight));
        var closeWidth = Math.Max(closeSize.Width + 12, 90);
        closeButton.Frame = new CGRect(availableRight - closeWidth, y, closeWidth, ButtonHeight);
        availableRight -= (nfloat)closeWidth + 8;

        if (copyButton != null)
        {
            var copySize = copyButton.SizeThatFits(new CGSize(width, ButtonHeight));
            var copyWidth = Math.Max(copySize.Width + 12, 80);
            copyButton.Frame = new CGRect(availableRight - copyWidth, y, copyWidth, ButtonHeight);
            availableRight -= (nfloat)copyWidth + 8;
        }

        var overlaySize = overlayButton.SizeThatFits(new CGSize(width, ButtonHeight));
        var overlayWidth = Math.Max(overlaySize.Width + 12, 90);
        overlayButton.Frame = new CGRect(availableRight - overlayWidth, y, overlayWidth, ButtonHeight);

        var titleSize = titleLabel.SizeThatFits(new CGSize(width, ButtonHeight));
        var titleY = y + (ButtonHeight - titleSize.Height) / 2;
        titleLabel.Frame = new CGRect(HorizontalPadding, titleY, titleSize.Width, titleSize.Height);

        var headerBottom = y + ButtonHeight;
        y = headerBottom + SectionSpacing;

        chart.Frame = new CGRect(0, y, width, ChartHeight);
        y += ChartHeight;

        var tableHeight = Bounds.Height - safe.Bottom - y;
        table.Frame = new CGRect(0, y, width, (nfloat)Math.Max(0, tableHeight));
    }

    private static UIColor ToUiColor(Color color)
    {
        return UIColor.FromRGBA(color.RedNormalized, color.GreenNormalized, color.BlueNormalized, color.AlphaNormalized);
    }

    private static UIButton CreatePillButton(string text)
    {
        var button = new UIButton(UIButtonType.System);
        button.SetTitle(text, UIControlState.Normal);
        button.SetTitleColor(UIColor.White, UIControlState.Normal);
        button.TitleLabel.Font = UIFont.BoldSystemFontOfSize(14);
        button.BackgroundColor = ToUiColor(MaudeConstants.MaudeBrandColor_Faded);
        button.Layer.CornerRadius = 10;
        button.ClipsToBounds = true;
        button.ContentEdgeInsets = new UIEdgeInsets(6, 12, 6, 12);
        return button;
    }

    private static void ToggleOverlay()
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

    private static async Task ExecuteSaveSnapshotAsync(IMaudeDataSink dataSink, MaudeSaveSnapshotAction action)
    {
        try
        {
            var snapshot = dataSink.Snapshot();
            await action.CopyDelegate(snapshot);
        }
        catch (Exception ex)
        {
            MaudeLogger.Error("Failed to execute save snapshot action.");
            MaudeLogger.Exception(ex);
        }
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

internal sealed class DismissablePresentationDelegate : UIAdaptivePresentationControllerDelegate
{
    private readonly Action onDismissed;

    public DismissablePresentationDelegate(Action onDismissed)
    {
        this.onDismissed = onDismissed;
    }

    public override bool ShouldDismiss(UIPresentationController presentationController) => true;

    public override void DidDismiss(UIPresentationController presentationController)
    {
        onDismissed();
    }
}
#endif
