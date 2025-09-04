using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Ansight.Adb.Telemetry;
using Ansight.IOC;
using Ansight.Studio.Telemetry;
using Ansight.Studio.UI.Timeline;
using Ansight.TimeZones;
using Ansight.Utilities;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace Maude.Runtime.Views.Telemetry
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TelemetryChartCollection : StackLayout
    {
        IMainThread MainThread => Resolver.Resolve<IMainThread>();

        private readonly IChartRendererFactory chartRendererFactory;
        private readonly ITelemetryPreferences telemetryPreferences;


        public TelemetryChartCollection()
        {
            InitializeComponent();

            chartRendererFactory = Resolver.Resolve<IChartRendererFactory>();
            telemetryPreferences = Resolver.Resolve<ITelemetryPreferences>();

            telemetryPreferences.TelemetryExcludedGroupPreferencesChanged += TelemetryPreferences_TelemetryExcludedGroupPreferencesChanged;
        }

        private void TelemetryPreferences_TelemetryExcludedGroupPreferencesChanged(object sender, TelemetryExcludedGroupsPreferencesChangedEventArgs e)
        {
            var targetCharts = this.Children.OfType<TelemetryChart>()
                                            .Where(chart => chart.TelemetryChannel != null && chart.TelemetryChannel.Name == e.Channel)
                                            .ToList();

            foreach (var chart in targetCharts)
            {
                chart.ExcludedGroups = e.ExcludedGroups;
            }
        }

        public IReadOnlyList<TelemetryChart> Charts => Children.OfType<TelemetryChart>().ToList();

        public static readonly BindableProperty IsChartClickEnabledProperty = BindableProperty.Create(nameof(IsChartClickEnabled), typeof(bool), typeof(TelemetryChartCollection), default(bool));
        public bool IsChartClickEnabled
        {
            get => (bool)GetValue(IsChartClickEnabledProperty);
            set => SetValue(IsChartClickEnabledProperty, value);
        }

        public static readonly BindableProperty IsChartDoubleClickEnabledProperty = BindableProperty.Create(nameof(IsChartDoubleClickEnabled), typeof(bool), typeof(TelemetryChartCollection), default(bool));
        public bool IsChartDoubleClickEnabled
        {
            get => (bool)GetValue(IsChartDoubleClickEnabledProperty);
            set => SetValue(IsChartDoubleClickEnabledProperty, value);
        }

        public static readonly BindableProperty TelemetryTimeDisplayModeProperty = BindableProperty.Create(nameof(TelemetryTimeDisplayMode), typeof(TelemetryTimeDisplayMode), typeof(TelemetryChartCollection), default(TelemetryTimeDisplayMode), propertyChanged: OnTelemetryTimeDisplayModeChanged);
        public TelemetryTimeDisplayMode TelemetryTimeDisplayMode
        {
            get => (TelemetryTimeDisplayMode)GetValue(TelemetryTimeDisplayModeProperty);
            set => SetValue(TelemetryTimeDisplayModeProperty, value);
        }

        public static readonly BindableProperty ChartClickedCommandProperty = BindableProperty.Create(nameof(ChartClickedCommand), typeof(ICommand), typeof(TelemetryChartCollection), default(ICommand));
        public ICommand ChartClickedCommand
        {
            get => (ICommand)GetValue(ChartClickedCommandProperty);
            set => SetValue(ChartClickedCommandProperty, value);
        }

        public static readonly BindableProperty ChartDoubleClickedCommandProperty = BindableProperty.Create(nameof(ChartDoubleClickedCommand), typeof(ICommand), typeof(TelemetryChartCollection), default(ICommand));
        public ICommand ChartDoubleClickedCommand
        {
            get => (ICommand)GetValue(ChartDoubleClickedCommandProperty);
            set => SetValue(ChartDoubleClickedCommandProperty, value);
        }

        static void OnTelemetryTimeDisplayModeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChartCollection control)
            {
                var value = (TelemetryTimeDisplayMode)newValue;
                control.ApplyTimeDisplayMode(value);
            }
        }

        private void ApplyTimeDisplayMode(TelemetryTimeDisplayMode displayMode)
        {
            foreach (var chart in Charts)
            {
                chart.TelemetryTimeDisplayMode = displayMode;
            }
        }

        public static readonly BindableProperty DateTimeFormatStringProperty = BindableProperty.Create(nameof(DateTimeFormatString), typeof(string), typeof(TelemetryChartCollection), default(string), propertyChanged: OnDateTimeFormatStringChanged);
        public string DateTimeFormatString
        {
            get => (string)GetValue(DateTimeFormatStringProperty);
            set => SetValue(DateTimeFormatStringProperty, value);
        }

        static void OnDateTimeFormatStringChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChartCollection control)
            {
                var value = (string)newValue;
                control.ApplyDateTimeFormatString(value);
            }
        }

        private void ApplyDateTimeFormatString(string dateTimeFormatString)
        {
            foreach (var chart in Charts)
            {
                chart.DateTimeFormatString = dateTimeFormatString;
            }
        }

        public static readonly BindableProperty TelemetryStartTimeUtcProperty = BindableProperty.Create(nameof(TelemetryStartTimeUtc), typeof(DateTime), typeof(TelemetryChartCollection), default(DateTime), propertyChanged: OnTelemetryStartTimeUtcChanged);
        public DateTime TelemetryStartTimeUtc
        {
            get => (DateTime)GetValue(TelemetryStartTimeUtcProperty);
            set => SetValue(TelemetryStartTimeUtcProperty, value);
        }

        static void OnTelemetryStartTimeUtcChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChartCollection control)
            {
                var value = (DateTime)newValue;
                control.ApplyStartTimeUtc(value);
            }
        }

        private void ApplyStartTimeUtc(DateTime value)
        {
            foreach (var chart in Charts)
            {
                chart.TelemetryStartTimeUtc = value;
            }
        }

        public static readonly BindableProperty ChartAxesLabelsProperty = BindableProperty.Create(nameof(ChartAxesLabels), typeof(ChartAxesLabels), typeof(TelemetryChartCollection), default(ChartAxesLabels), propertyChanged: OnChartAxesLabelsChanged);
        public ChartAxesLabels ChartAxesLabels
        {
            get => (ChartAxesLabels)GetValue(ChartAxesLabelsProperty);
            set => SetValue(ChartAxesLabelsProperty, value);
        }

        static void OnChartAxesLabelsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChartCollection control)
            {
                var value = (ChartAxesLabels)newValue;
                control.ApplyAxesLabels(value);
            }
        }

        private void ApplyAxesLabels(ChartAxesLabels labels)
        {
            foreach (var chart in Charts)
            {
                chart.AxisLabel = labels.GetAxisLabelForChannel(chart.Title);
            }
        }

        public static readonly BindableProperty TimeZoneProperty = BindableProperty.Create(nameof(TimeZone), typeof(ITimeZone), typeof(TelemetryViewer), default(ITimeZone), propertyChanged: OnTimeZoneChanged);
        public ITimeZone TimeZone
        {
            get => (ITimeZone)GetValue(TimeZoneProperty);
            set => SetValue(TimeZoneProperty, value);
        }

        static void OnTimeZoneChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChartCollection control)
            {
                var value = (ITimeZone)newValue;
                control.ApplyTimeZone(value);
            }
        }

        private void ApplyTimeZone(ITimeZone timeZone)
        {
            foreach (var chart in Charts)
            {
                chart.TimeZone = timeZone;
            }
        }

        public static readonly BindableProperty ShouldShowPositionProperty = BindableProperty.Create(nameof(ShouldShowPosition), typeof(bool), typeof(TelemetryChartCollection), true, propertyChanged: OnShouldShowPositionChanged);
        public bool ShouldShowPosition
        {
            get => (bool)GetValue(ShouldShowPositionProperty);
            set => SetValue(ShouldShowPositionProperty, value);
        }

        static void OnShouldShowPositionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChartCollection control)
            {
                var value = (bool)newValue;
                control.ApplyPosition(control.Position, value);
            }
        }

        public static readonly BindableProperty HoverPositionProperty = BindableProperty.Create(nameof(HoverPosition), typeof(Point?), typeof(TelemetryChartCollection), default(Point?), propertyChanged: OnHoverPositionChanged);
        public Point? HoverPosition
        {
            get => (Point?)GetValue(HoverPositionProperty);
            set => SetValue(HoverPositionProperty, value);
        }

        static void OnHoverPositionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChartCollection control)
            {
                control.ApplyHoverPosition((Point?)newValue, control.ShouldShowPosition);
            }
        }

        private void ApplyHoverPosition(Point? hoverPosition, bool shouldShowPosition)
        {
            foreach (var chart in Charts)
            {
                chart.HoverPosition = hoverPosition;
            }
        }

        public static readonly BindableProperty PositionProperty = BindableProperty.Create(nameof(Position), typeof(DateTime), typeof(TelemetryChartCollection), default(DateTime), propertyChanged: OnPositionChanged);
        public DateTime Position
        {
            get => (DateTime)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        static void OnPositionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChartCollection control)
            {
                control.ApplyPosition((DateTime)newValue, control.ShouldShowPosition);
            }
        }

        private void ApplyPosition(DateTime position, bool shouldShowPosition)
        {
            foreach (var chart in Charts)
            {
                chart.Position = position;
                chart.ShouldShowPosition = shouldShowPosition;
            }
        }

        public static readonly BindableProperty TelemetrySourceProperty = BindableProperty.Create(nameof(TelemetrySource), typeof(ITelemetrySource), typeof(TelemetryViewer), default(ITelemetrySource), propertyChanged: OnTelemetrySourceChanged);
        public ITelemetrySource TelemetrySource
        {
            get => (ITelemetrySource)GetValue(TelemetrySourceProperty);
            set => SetValue(TelemetrySourceProperty, value);
        }

        static void OnTelemetrySourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is TelemetryChartCollection control)
            {
                if (oldValue is ITelemetrySource oldTelemetrySource)
                {
                    control.UnbindEvents(oldTelemetrySource);
                }

                var newTelemetrySource = newValue as ITelemetrySource;
                if (newTelemetrySource != null)
                {
                    control.BindEvents(newTelemetrySource);
                }
                control.Apply(newTelemetrySource?.Sink);
            }
        }

        private void UnbindEvents(ITelemetrySource telemetrySource)
        {
            if (telemetrySource is null)
            {
                throw new ArgumentNullException(nameof(telemetrySource));
            }

            if (telemetrySource.Sink != null)
            {
                telemetrySource.Sink.OnChannelsAdded -= Sink_OnChannelsChanged;
                telemetrySource.Sink.OnChannelsRemoved -= Sink_OnChannelsChanged;
            }

            if (telemetrySource is IReplayingTelemetrySource replayingTelemetrySource)
            {
                replayingTelemetrySource.OnTelemetryLoaded -= ReplayingTelemetrySource_OnTelemetryLoaded;
            }
        }

        private void ReplayingTelemetrySource_OnTelemetryLoaded(object sender, ReplayingTelemetrySourceLoadedEventArgs e)
        {
            Apply(e.TelemetrySource.Sink);
        }

        private void Sink_OnChannelsChanged(object sender, Adb.Telemetry.TelemetryChannelsChangedEventArgs e)
        {
            Apply(e.TelementrySink);
        }

        private void Apply(ITelemetrySink telementrySink)
        {
            if (!MainThread.IsMainThread)
            {
                MainThread.InvokeOnMainThread(() => Apply(telementrySink));
                return;
            }

            ClearCharts();

            if (telementrySink != null && telementrySink.Channels.Count > 0)
            {
                Apply(telementrySink.Channels,
                      TimeZone,
                      ChartAxesLabels,
                      Position,
                      ShouldShowPosition,
                      TelemetryStartTimeUtc,
                      TelemetryTimeDisplayMode,
                      DateTimeFormatString);
            }
        }

        private void Apply(IReadOnlyList<ITelemetryChannel> channels,
                           ITimeZone timeZone,
                           ChartAxesLabels labels,
                           DateTime positionUtc,
                           bool shouldShowPosition,
                           DateTime telemetryStartTimeUtc,
                           TelemetryTimeDisplayMode timeDisplayMode,
                           string dateTimeFormatString)
        {
            if (channels is null)
            {
                return;
            }

            labels = labels ?? ChartAxesLabels.Empty;

            foreach (var channel in channels)
            {
                var renderer = chartRendererFactory.Create(channel);
                var chart = new TelemetryChart(renderer);
                chart.Title = channel.Name;
                chart.AxisLabel = labels.GetAxisLabelForChannel(channel.Name);
                chart.TimeZone = timeZone;
                chart.TelemetryChannel = channel;
                chart.Position = positionUtc;
                chart.TelemetryStartTimeUtc = telemetryStartTimeUtc;
                chart.TelemetryTimeDisplayMode = timeDisplayMode;
                chart.DateTimeFormatString = dateTimeFormatString;
                chart.ExcludedGroups = telemetryPreferences.GetExcludedGroupsForChannel(channel.Name);
                chart.OnChartHover += Chart_OnChartHover;
                chart.OnChartClicked += Chart_OnChartClicked;
                chart.OnChartDoubleClicked += Chart_OnChartDoubleClicked;
                chart.OnChannelGroupSelected += Chart_OnChannelGroupSelected;
                Children.Add(chart);
            }
        }

        private void Chart_OnChannelGroupSelected(object sender, ChannelGroupSelectedEventArgs e)
        {
            var channel = e.Channel;
            var group = e.Group;

            // Sanity checks
            if (string.IsNullOrWhiteSpace(channel)
                || string.IsNullOrWhiteSpace(group))
            {
                return;
            }

            if (e.IsGroupEnabled)
            {
                telemetryPreferences.AddExcludedGroup(channel, group);
            }
            else
            {
                telemetryPreferences.RemoveExcludedGroup(channel, group);
            }
        }

        private void Chart_OnChartHover(object sender, ChartHoverEventArgs e)
        {
            if (e.Event == ChartHoverEvent.Ended)
            {
                this.HoverPosition = null;
            }
            else
            {
                this.HoverPosition = e.ViewLocation;
            }
        }

        private void ClearCharts()
        {
            var chartsCopy = Charts.ToList();
            Children.Clear();

            foreach (var chart in chartsCopy)
            {
                chart.OnChartClicked -= Chart_OnChartClicked;
                chart.OnChartDoubleClicked -= Chart_OnChartDoubleClicked;
                chart.OnChartHover -= Chart_OnChartHover;
                chart.OnChannelGroupSelected -= Chart_OnChannelGroupSelected;
                chart.Dispose();
            }
        }

        private void Chart_OnChartDoubleClicked(object sender, ChartClickedEventArgs e)
        {
            if (!IsChartDoubleClickEnabled)
            {
                return;
            }

            if (ChartDoubleClickedCommand != null && ChartDoubleClickedCommand.CanExecute(e.ChartPosition))
            {
                ChartDoubleClickedCommand.Execute(e.ChartPosition);
            }
        }

        private void Chart_OnChartClicked(object sender, ChartClickedEventArgs e)
        {
            if (!IsChartClickEnabled)
            {
                return;
            }

            if (ChartClickedCommand != null && ChartClickedCommand.CanExecute(e.ChartPosition))
            {
                ChartClickedCommand.Execute(e.ChartPosition);
            }
        }

        private void BindEvents(ITelemetrySource telemetrySource)
        {
            if (telemetrySource is null)
            {
                throw new ArgumentNullException(nameof(telemetrySource));
            }

            UnbindEvents(telemetrySource);

            if (telemetrySource.Sink != null)
            {
                telemetrySource.Sink.OnChannelsAdded += Sink_OnChannelsChanged;
                telemetrySource.Sink.OnChannelsRemoved += Sink_OnChannelsChanged;
            }

            if (telemetrySource is IReplayingTelemetrySource replayingTelemetrySource)
            {
                replayingTelemetrySource.OnTelemetryLoaded += ReplayingTelemetrySource_OnTelemetryLoaded;
            }
        }
    }
}

