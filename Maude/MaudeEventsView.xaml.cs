using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Maude;

/// <summary>
/// Displays the recent Maude events as a list inside the slide-in sheet.
/// </summary>
public partial class MaudeEventsView : ContentView
{
    private const int MaxEvents = 50;
    private IMaudeDataSink dataSink;

    public ObservableCollection<MaudeEventDisplay> VisibleEvents { get; } = new();

    public MaudeEventsView()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public static readonly BindableProperty DataSinkProperty = BindableProperty.Create(nameof(DataSink),
                                                                                       typeof(IMaudeDataSink),
                                                                                       typeof(MaudeEventsView),
                                                                                       null,
                                                                                       propertyChanged: OnDataSinkChanged);

    public IMaudeDataSink DataSink
    {
        get => (IMaudeDataSink)GetValue(DataSinkProperty);
        set => SetValue(DataSinkProperty, value);
    }

    private static void OnDataSinkChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MaudeEventsView view)
        {
            if (oldValue is IMaudeDataSink oldSink)
            {
                view.Unsubscribe(oldSink);
            }

            if (newValue is IMaudeDataSink newSink)
            {
                view.Subscribe(newSink);
            }
        }
    }

    private void Subscribe(IMaudeDataSink sink)
    {
        dataSink = sink;
        if (sink != null)
        {
            dataSink.OnEventsUpdated += HandleEventsUpdated;
        }

        RefreshEvents();
    }

    private void Unsubscribe(IMaudeDataSink sink)
    {
        if (sink != null)
        {
            sink.OnEventsUpdated -= HandleEventsUpdated;
        }

        if (ReferenceEquals(dataSink, sink))
        {
            dataSink = null;
        }
    }

    private void HandleEventsUpdated(object sender, MaudeEventsUpdatedEventArgs e)
    {
        RefreshEvents();
    }

    private void RefreshEvents()
    {
        if (Dispatcher?.IsDispatchRequired == true)
        {
            Dispatcher.Dispatch(RefreshEvents);
            return;
        }

        VisibleEvents.Clear();
        var sink = dataSink;
        if (sink == null)
        {
            return;
        }

        var channelLookup = sink.Channels?.ToDictionary(c => c.Id) ?? new Dictionary<byte, MaudeChannel>();

        foreach (var maudeEvent in sink.Events.OrderByDescending(e => e.CapturedAtUtc).Take(MaxEvents))
        {
            if (!channelLookup.TryGetValue(maudeEvent.Channel, out var channel))
            {
                continue;
            }

            VisibleEvents.Add(new MaudeEventDisplay()
            {
                Label = maudeEvent.Label,
                Icon = string.IsNullOrWhiteSpace(maudeEvent.Icon) ? MaterialSymbols.Info : maudeEvent.Icon,
                ChannelColor = channel.Color,
                Details = maudeEvent.Details,
                HasDetails = !string.IsNullOrWhiteSpace(maudeEvent.Details),
                Timestamp = maudeEvent.CapturedAtUtc.ToLocalTime().ToString("HH:mm:ss")
            });
        }
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        if (args.NewHandler == null)
        {
            Unsubscribe(dataSink);
        }
    }

    /// <summary>
    /// UI-facing projection of a <see cref="MaudeEvent"/> for binding.
    /// </summary>
    public class MaudeEventDisplay
    {
        public string Icon { get; init; }
        public string Label { get; init; }
        public string Details { get; init; }
        public bool HasDetails { get; init; }
        public Color ChannelColor { get; init; }
        public string Timestamp { get; init; }
    }

    public void Detach()
    {
        Unsubscribe(dataSink);
    }
}
