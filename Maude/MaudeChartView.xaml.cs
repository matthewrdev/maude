using SkiaSharp;
using SkiaSharp.Views.Maui;
using Microsoft.Maui.Devices;

namespace Maude;

/// <summary>
/// Renders Maude metrics and events as a live chart inside the slide-in sheet or overlay.
/// </summary>
public partial class MaudeChartView : VerticalStackLayout
{
    private IMaudeDataSink dataSink;
    private IDispatcherTimer redrawTimer;
    private float? probeRatio;
    private SKRect? chartBounds;

    public MaudeChartView()
    {
        InitializeComponent();
        titleLabel.TextColor = MaudeConstants.MaudeBrandColor;
        canvasView.EnableTouchEvents = true;
        canvasView.Touch += OnCanvasTouch;
        InitialiseTimer();
        UpdateWindowLabel();
        UpdateModeVisuals();
    }

    public static readonly BindableProperty WindowDurationProperty = BindableProperty.Create(nameof(WindowDuration),
                                                                                             typeof(TimeSpan),
                                                                                             typeof(MaudeChartView),
                                                                                             TimeSpan.FromMinutes(1),
                                                                                             propertyChanged: OnWindowDurationChanged);

    public TimeSpan WindowDuration
    {
        get => (TimeSpan)GetValue(WindowDurationProperty);
        set => SetValue(WindowDurationProperty, value);
    }

    private static void OnWindowDurationChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MaudeChartView view)
        {
            view.UpdateWindowLabel();
            view.RequestRedraw();
        }
    }

    public static readonly BindableProperty DataSinkProperty = BindableProperty.Create(nameof(DataSink),
                                                                                       typeof(IMaudeDataSink),
                                                                                       typeof(MaudeChartView),
                                                                                       null,
                                                                                       propertyChanged: OnDataSinkChanged);

    public static readonly BindableProperty RenderModeProperty = BindableProperty.Create(nameof(RenderMode),
                                                                                         typeof(MaudeChartRenderMode),
                                                                                         typeof(MaudeChartView),
                                                                                         MaudeChartRenderMode.Inline,
                                                                                         propertyChanged: OnRenderModeChanged);

    public IMaudeDataSink DataSink
    {
        get => (IMaudeDataSink)GetValue(DataSinkProperty);
        set => SetValue(DataSinkProperty, value);
    }

    public MaudeChartRenderMode RenderMode
    {
        get => (MaudeChartRenderMode)GetValue(RenderModeProperty);
        set => SetValue(RenderModeProperty, value);
    }

    private static void OnDataSinkChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MaudeChartView view)
        {
            if (oldValue is IMaudeDataSink oldDataSink)
            {
                view.Unsubscribe(oldDataSink);
            }

            if (newValue is IMaudeDataSink newDataSink)
            {
                view.Subscribe(newDataSink);
            }
        }
    }

    private static void OnRenderModeChanged(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (bindable is MaudeChartView view)
        {
            view.UpdateModeVisuals();
            view.RequestRedraw();
        }
    }

    private void UpdateModeVisuals()
    {
        var isOverlay = RenderMode == MaudeChartRenderMode.Overlay;
        var overlayScale = DeviceInfo.Current.Platform == DevicePlatform.Android ? 1f : 0.5f;

        this.Opacity = isOverlay ? 0.85 : 1;
        this.Scale = isOverlay ? overlayScale : 1f;
        this.InputTransparent = isOverlay;
        this.IsEnabled = !isOverlay;
        this.Spacing = isOverlay ? 0 : 8;
        if (headerLayout != null)
        {
            headerLayout.IsVisible = !isOverlay;
        }

        if (canvasView != null)
        {
            canvasView.EnableTouchEvents = !isOverlay;
            var overlayHeight = DeviceInfo.Current.Platform == DevicePlatform.Android ? 180 : 120;
            canvasView.HeightRequest = isOverlay ? overlayHeight : 220;
        }

        if (isOverlay)
        {
            probeRatio = null;
        }
    }

    private void Subscribe(IMaudeDataSink sink)
    {
        if (this.dataSink != null)
        {
            sink.OnMetricsUpdated -= HandleMetricsUpdated;
            sink.OnEventsUpdated -= HandleEventsUpdated;
        }
        
        dataSink = sink;
        
        dataSink.OnMetricsUpdated += HandleMetricsUpdated;
        dataSink.OnEventsUpdated += HandleEventsUpdated;
        RequestRedraw();
    }

    private void Unsubscribe(IMaudeDataSink sink)
    {
        sink.OnMetricsUpdated -= HandleMetricsUpdated;
        sink.OnEventsUpdated -= HandleEventsUpdated;
        if (ReferenceEquals(dataSink, sink))
        {
            dataSink = null;
        }
    }

    private void HandleMetricsUpdated(object sender, MaudeMetricsUpdatedEventArgs e)
    {
        RequestRedraw();
    }

    private void HandleEventsUpdated(object sender, MaudeEventsUpdatedEventArgs e)
    {
        RequestRedraw();
    }

    private void RequestRedraw()
    {
        if (Dispatcher?.IsDispatchRequired == true)
        {
            Dispatcher.Dispatch(RequestRedraw);
            return;
        }

        canvasView?.InvalidateSurface();
    }

    private void UpdateWindowLabel()
    {
        var seconds = Math.Max(1, (int)Math.Round(WindowDuration.TotalSeconds));
        titleLabel.Text = $"MEMORY OVERVIEW (Last {seconds}s)";
    }

    private void InitialiseTimer()
    {
        var dispatcher = Dispatcher ?? Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            return;
        }

        redrawTimer = dispatcher.CreateTimer();
        redrawTimer.Interval = TimeSpan.FromMilliseconds(500);
        redrawTimer.Tick += OnRedrawTimerOnTick;
        redrawTimer.Start();
    }

    private void OnRedrawTimerOnTick(object? o, EventArgs eventArgs)
    {
        UpdateWindowLabel();
        RequestRedraw();
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var sink = dataSink;
        if (sink == null)
        {
            e.Surface.Canvas.Clear();
            chartBounds = null;
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
            ProbePosition = RenderMode == MaudeChartRenderMode.Inline ? probeRatio : null
        };

        var renderResult = MaudeChartRenderer.Render(e.Surface.Canvas, e.Info, sink, renderOptions);
        UpdateChartBounds(renderResult);
    }

    private void UpdateChartBounds(MaudeRenderResult renderResult)
    {
        if (renderResult.HasChartArea && renderResult.ChartBounds.Width > 0 && renderResult.ChartBounds.Height > 0)
        {
            chartBounds = renderResult.ChartBounds;
        }
        else
        {
            chartBounds = null;
        }
    }

    private void OnCanvasTouch(object? sender, SKTouchEventArgs e)
    {
        if (RenderMode != MaudeChartRenderMode.Inline)
        {
            probeRatio = null;
            return;
        }

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
            case SKTouchAction.Moved:
                var chartRect = chartBounds;
                if (!chartRect.HasValue || chartRect.Value.Width <= 0)
                {
                    break;
                }

                var relativeX = (e.Location.X - chartRect.Value.Left) / chartRect.Value.Width;
                var ratio = (float)Math.Clamp(relativeX, 0d, 1d);
                if (!float.IsNaN(ratio))
                {
                    probeRatio = ratio;
                    e.Handled = true;
                    RequestRedraw();
                }
                break;
            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                probeRatio = null;
                e.Handled = true;
                RequestRedraw();
                break;
        }
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        if (args.NewHandler == null)
        {
            Detach();
        }
    }

    public void Detach()
    {
        if (redrawTimer != null)
        {
            redrawTimer.Stop();
            redrawTimer.Tick -= OnRedrawTimerOnTick;
        }

        redrawTimer = null;

        if (dataSink != null)
        {
            Unsubscribe(dataSink);
        }

        if (canvasView != null)
        {
            canvasView.Touch -= OnCanvasTouch;
        }

        chartBounds = null;
    }
}
