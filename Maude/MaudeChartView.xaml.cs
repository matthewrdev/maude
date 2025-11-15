using SkiaSharp.Views.Maui;

namespace Maude;

public partial class MaudeChartView : VerticalStackLayout
{
    private IMaudeDataSink dataSink;
    private IDispatcherTimer redrawTimer;

    public MaudeChartView()
    {
        InitializeComponent();
        InitialiseTimer();
        UpdateWindowLabel();
    }

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title),
                                                                                    typeof(string),
                                                                                    typeof(MaudeChartView),
                                                                                    "Memory overview",
                                                                                    propertyChanged: OnTitleChanged);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MaudeChartView view && newValue is string text)
        {
            view.titleLabel.Text = text;
        }
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

    public IMaudeDataSink DataSink
    {
        get => (IMaudeDataSink)GetValue(DataSinkProperty);
        set => SetValue(DataSinkProperty, value);
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
        windowLabel.Text = $"Last {seconds}s";
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
            CurrentUtc = now
        };

        MaudeChartRenderer.Render(e.Surface.Canvas, e.Info, sink, renderOptions);
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        if (args.NewHandler == null)
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
        }
    }
}
