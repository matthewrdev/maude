using System;
using System.Collections.Generic;
using System.Linq;
using Ansight.Adb.Telemetry;
using Ansight.IOC;
using Ansight.Studio.Telemetry;
using Ansight.Studio.UI.Controls;
using Ansight.TimeZones;
using Microsoft.Maui.Platform;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using UIKit;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Maude.Runtime.Views.Telemetry
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TelemetryChart : VerticalStackLayout
    {
        public event EventHandler<ChannelGroupSelectedEventArgs> OnChannelGroupSelected;

        private readonly IMainThread mainThread;
        private readonly IChartRenderer renderer;

        public event EventHandler<ChartHoverEventArgs> OnChartHover;
        public event EventHandler<ChartClickedEventArgs> OnChartClicked;
        public event EventHandler<ChartClickedEventArgs> OnChartDoubleClicked;

        public TelemetryChart(IChartRenderer renderer)
        {
            InitializeComponent();

            mainThread = Resolver.Resolve<IMainThread>();
            this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

            InitialiseNative();
        }

        void SKCanvasView_PaintSurface(System.Object sender, SKPaintSurfaceEventArgs e)
        {
            renderer.Render(e.Surface,
                            e.Info.Width,
                            e.Info.Height,
                            Position,
                            ShouldShowPosition,
                            HoverPosition,
                            TimeZone,
                            TelemetryStartTimeUtc,
                            TelemetryTimeDisplayMode,
                            AxisLabel,
                            DateTimeFormatString,
                            VerticalAxisIntervalAmount,
                            TelemetryChannel,
                            ExcludedGroups);
        }

        const string DefaultDateTimeFormatString = "mm:ss.ff";

        public static readonly BindableProperty DateTimeFormatStringProperty = BindableProperty.Create(nameof(DateTimeFormatString), typeof(string), typeof(TelemetryChart), DefaultDateTimeFormatString, propertyChanged: OnDateTimeFormatStringChanged);
        public string DateTimeFormatString
        {
            get => (string)GetValue(DateTimeFormatStringProperty);
            set => SetValue(DateTimeFormatStringProperty, value);
        }

        static void OnDateTimeFormatStringChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChart control)
            {
                control.RequestRedraw();
            }
        }

        public static readonly BindableProperty TelemetryStartTimeUtcProperty = BindableProperty.Create(nameof(TelemetryStartTimeUtc), typeof(DateTime), typeof(TelemetryChart), default(DateTime), propertyChanged: OnTelemetryStartTimeUtcChanged);
        public DateTime TelemetryStartTimeUtc
        {
            get => (DateTime)GetValue(TelemetryStartTimeUtcProperty);
            set => SetValue(TelemetryStartTimeUtcProperty, value);
        }

        static void OnTelemetryStartTimeUtcChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChart control)
            {
                control.RequestRedraw();
            }
        }

        public static readonly BindableProperty TelemetryTimeDisplayModeProperty = BindableProperty.Create(nameof(TelemetryTimeDisplayMode), typeof(TelemetryTimeDisplayMode), typeof(TelemetryChart), default(TelemetryTimeDisplayMode), propertyChanged: OnTelemetryTimeDisplayModeChanged);
        public TelemetryTimeDisplayMode TelemetryTimeDisplayMode
        {
            get => (TelemetryTimeDisplayMode)GetValue(TelemetryTimeDisplayModeProperty);
            set => SetValue(TelemetryTimeDisplayModeProperty, value);
        }

        static void OnTelemetryTimeDisplayModeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChart control)
            {
                control.RequestRedraw();
            }
        }

        public static readonly BindableProperty TimeZoneProperty = BindableProperty.Create(nameof(TimeZone), typeof(ITimeZone), typeof(TelemetryChart), default(ITimeZone), propertyChanged: OnTimeZoneChanged);
        public ITimeZone TimeZone
        {
            get => (ITimeZone)GetValue(TimeZoneProperty);
            set => SetValue(TimeZoneProperty, value);
        }

        static void OnTimeZoneChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChart control)
            {
                control.RequestRedraw();
            }
        }

        public static readonly BindableProperty ExcludedGroupsProperty = BindableProperty.Create(nameof(ExcludedGroups), typeof(IReadOnlyList<string>), typeof(TelemetryChart), default(IReadOnlyList<string>), propertyChanged: OnExcludedGroupsChanged);
        public IReadOnlyList<string> ExcludedGroups
        {
            get => (IReadOnlyList<string>)GetValue(ExcludedGroupsProperty);
            set => SetValue(ExcludedGroupsProperty, value);
        }

        static void OnExcludedGroupsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChart control)
            {
                control.RequestRedraw();
                control.ApplyLegend(control.TelemetryChannel, newValue as IReadOnlyList<string>);
            }
        }

        public static readonly BindableProperty AxisLabelProperty = BindableProperty.Create(nameof(AxisLabel), typeof(string), typeof(TelemetryChart), default(string), propertyChanged: OnAxisLabelChanged);

        /// <summary>
        /// The name of the vertical axis measurement.
        /// </summary>
        public string AxisLabel
        {
            get => (string)GetValue(AxisLabelProperty);
            set => SetValue(AxisLabelProperty, value);
        }

        static void OnAxisLabelChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChart control)
            {
                control.RequestRedraw();
            }
        }

        public static readonly BindableProperty VerticalAxisIntervalAmountProperty = BindableProperty.Create(nameof(VerticalAxisIntervalAmount), typeof(int), typeof(TelemetryChart), 1, propertyChanged: OnVerticalAxisIntervalAmountChanged);
        public int VerticalAxisIntervalAmount
        {
            get => (int)GetValue(VerticalAxisIntervalAmountProperty);
            set => SetValue(VerticalAxisIntervalAmountProperty, value);
        }

        static void OnVerticalAxisIntervalAmountChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChart control)
            {
                control.RequestRedraw();
            }
        }

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(TelemetryChart), default(string), propertyChanged: OnTitleChanged);
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChart control)
            {
                control.RequestRedraw();
            }
        }

        public static readonly BindableProperty TelemetryChannelProperty = BindableProperty.Create(nameof(TelemetryChannel), typeof(ITelemetryChannel), typeof(TelemetryChart), default(ITelemetryChannel), propertyChanged: OnTelemetryChannelChanged);
        public ITelemetryChannel TelemetryChannel
        {
            get => (ITelemetryChannel)GetValue(TelemetryChannelProperty);
            set => SetValue(TelemetryChannelProperty, value);
        }

        static void OnTelemetryChannelChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChart control)
            {
                if (oldValue is ITelemetryChannel oldTelemetryChannel)
                {
                    control.UnbindEvents(oldTelemetryChannel);
                }

                if (newValue is ITelemetryChannel newTelemetryChannel)
                {
                    control.BindEvents(newTelemetryChannel);
                    control.RequestRedraw();
                    control.ApplyLegend(newTelemetryChannel, control.ExcludedGroups);
                }
            }
        }

        private void ApplyLegend(ITelemetryChannel channel, IReadOnlyList<string> excludedGroups)
        {
            excludedGroups = excludedGroups ?? Array.Empty<string>();
            var legendItems = new List<LegendItem>();
            if (channel != null)
            {
                legendItems = channel.Groups.Select(group => new LegendItem()
                {
                    Color = this.renderer.Options.GetGroupColor(group).ToMauiColor(),
                    Label = group,
                    Enabled = excludedGroups.Contains(group) ? false : true
                }).ToList();
            }

            this.legendView.Items = legendItems;
        }

        private void Channel_OnTelemetrySegmentsClosed(object sender, TelemetrySegmentsChangedEventArgs e)
        {
            foreach (var segment in e.Segments)
            {
                BindEvents(segment);
            }
            RequestRedraw();
        }

        private void Channel_OnTelemetrySegmentsOpened(object sender, TelemetrySegmentsChangedEventArgs e)
        {
            foreach (var segment in e.Segments)
            {
                BindEvents(segment);
            }

            RequestRedraw();
        }

        private void BindEvents(ITelemetryChannel channel)
        {
            UnbindEvents(channel);

            channel.OnTelemetrySegmentsOpened += Channel_OnTelemetrySegmentsOpened;
            channel.OnTelemetrySegmentsClosed += Channel_OnTelemetrySegmentsClosed;

            foreach (var segment in channel.Segments)
            {
                BindEvents(segment);
            }
        }

        private void UnbindEvents(ITelemetryChannel channel)
        {
            channel.OnTelemetrySegmentsClosed -= Channel_OnTelemetrySegmentsClosed;
            channel.OnTelemetrySegmentsOpened -= Channel_OnTelemetrySegmentsOpened;

            foreach (var segment in channel.Segments)
            {
                UnbindEvents(segment);
            }
        }

        private void BindEvents(ITelemetrySegment segment)
        {
            UnbindEvents(segment);

            segment.OnDataPointsAdded += OnRedrawRequested;
            segment.OnDataPointsRemoved += OnRedrawRequested;
            segment.OnStartUtcChanged += OnRedrawRequested;
            segment.OnEndUtcChanged += OnRedrawRequested;
            segment.OnMinValueChanged += OnRedrawRequested;
            segment.OnMaxValueChanged += OnRedrawRequested;
        }

        private void UnbindEvents(ITelemetrySegment segment)
        {
            segment.OnDataPointsAdded -= OnRedrawRequested;
            segment.OnDataPointsRemoved -= OnRedrawRequested;
            segment.OnStartUtcChanged -= OnRedrawRequested;
            segment.OnEndUtcChanged -= OnRedrawRequested;
            segment.OnMinValueChanged -= OnRedrawRequested;
            segment.OnMaxValueChanged -= OnRedrawRequested;
        }

        public static readonly BindableProperty ShouldShowPositionProperty = BindableProperty.Create(nameof(ShouldShowPosition), typeof(bool), typeof(TelemetryChart), true, propertyChanged: OnShouldShowPositionChanged);
        public bool ShouldShowPosition
        {
            get => (bool)GetValue(ShouldShowPositionProperty);
            set => SetValue(ShouldShowPositionProperty, value);
        }

        static void OnShouldShowPositionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChart control)
            {
                control.RequestRedraw();
            }
        }

        public static readonly BindableProperty HoverPositionProperty = BindableProperty.Create(nameof(HoverPosition), typeof(Point?), typeof(TelemetryChart), default(Point?), propertyChanged: OnHoverPositionChanged);
        public Point? HoverPosition
        {
            get => (Point?)GetValue(HoverPositionProperty);
            set => SetValue(HoverPositionProperty, value);
        }

        static void OnHoverPositionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChart control)
            {
                control.RequestRedraw();
            }
        }

        public static readonly BindableProperty PositionProperty = BindableProperty.Create(nameof(Position), typeof(DateTime), typeof(TelemetryChart), default(DateTime), propertyChanged: OnPositionChanged);
        public DateTime Position
        {
            get => (DateTime)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        static void OnPositionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChart control)
            {
                control.RequestRedraw();
            }
        }

        private void OnRedrawRequested(object sender, EventArgs e)
        {
            RequestRedraw();
        }

        private void RequestRedraw()
        {
            if (!mainThread.IsMainThread)
            {
                mainThread.InvokeOnMainThread(RequestRedraw);
                return;
            }

            canvasView.InvalidateSurface();
        }

        public void Dispose()
        {
            if (TelemetryChannel != null)
            {
                UnbindEvents(this.TelemetryChannel);
            }
        }

        void legendView_LegendItemSelected(System.Object sender,
                                           Ansight.Studio.UI.Controls.LegendItemSelectedEventArgs e)
        {
            // Sanity checks
            if (this.TelemetryChannel is null
                || e.LegendItem is null
                || string.IsNullOrEmpty(e.LegendItem.Label))
            {
                return;
            }

            // More sanity checks...
            var containsGroup = this.TelemetryChannel.Groups.Contains(e.LegendItem.Label);
            if (!containsGroup)
            {
                return;
            }

            OnChannelGroupSelected?.Invoke(this, new ChannelGroupSelectedEventArgs(TelemetryChannel.Name,
                                                                                   e.LegendItem.Label,
                                                                                   e.LegendItem.Enabled));
        }
    }
}

