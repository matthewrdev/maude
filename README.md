# maude

Minimal in-app observability for .NET MAUI apps. Maude samples runtime memory, captures custom metrics/events, and presents a lightweight, Skia-rendered chart plus event feed as either a slide-in sheet or an embeddable view.

## Capabilities
- Runtime sampler for CLR (and platform) memory with configurable frequency/retention.
- Built-in channels for managed memory and platform memory; add your own channels for custom metrics/events.
- Popup presentation (sheet) of the Maude dashboard, or embed `MaudeView`, `MaudeChartView`, or `MaudeEventsView` directly in your UI.
- Event capture with optional icons; metrics capture with channel scoping.
- Thread-safe in-memory sink you can clear or read for your own visualisations.
- Simple lifecycle hooks to activate/deactivate sampling and present/dismiss the UI.

## Quick integration (default setup)
Add Maude to your MAUI app with minimal code.

1) Configure the app builder:
```csharp
// MauiProgram.cs
using Maude;

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder()
        .UseMauiApp<App>()
        .UseMaude<App>();  // wires fonts, initializes runtime if needed, registers services
    return builder.Build();
}
```

2) Activate sampling early (e.g., platform entry point):
```csharp
// Platforms/iOS/Program.cs (similar for Android)
MaudeRuntime.Activate();
```

3) Present Maude when needed (e.g., behind a debug button):
```csharp
MaudeRuntime.Present();   // slide-in sheet
// MaudeRuntime.Dismiss(); // close it
```

4) Send custom telemetry:
```csharp
MaudeRuntime.Metric(valueBytes, channelId);
MaudeRuntime.Event("A thing happened", channelId);
```

## Deep customization
Use custom channels/options and embed the views directly.

- Provide options (frequency, retention, channels) before calling `UseMaude`:
```csharp
var options = new MaudeOptions
{
    SampleFrequencyMilliseconds = 500,
    RetentionPeriodSeconds = 60 * 20,
    AdditionalChannels = new List<MaudeChannel>
    {
        new MaudeChannel(10, "Cache", new Color(255, 149, 0)),
        new MaudeChannel(11, "API Events", new Color(50, 173, 230))
    }
};

MaudeRuntime.Initialize(options);
builder.UseMaude<App>(); // will use the already-initialized runtime
```

- Attach to lifecycle for your own logging:
```csharp
MaudeRuntime.Instance.OnActivated += (_, _) => logger.LogInformation("Maude active");
MaudeRuntime.Instance.OnDeactivated += (_, _) => logger.LogInformation("Maude inactive");
```

- Embed the dashboard inline instead of (or as well as) the popup:
```xml
<maude:MaudeView />
<!-- Or individually: <maude:MaudeChartView /> + <maude:MaudeEventsView /> -->
```
Assign a sink manually if needed:
```csharp
chartView.DataSink = MaudeRuntime.Instance.DataSink;
eventsView.DataSink = MaudeRuntime.Instance.DataSink;
```

- Tune presentation manually:
```csharp
MaudeRuntime.Present();
MaudeRuntime.Dismiss();
MaudeRuntime.Deactivate(); // stop sampling (call Activate to resume)
MaudeRuntime.Clear();      // wipe metrics/events from the sink
```

## Notes
- Reserved channel IDs: `0` (.NET/CLR), `1` (platform), `255` (not specified); use other IDs for custom channels.
- Metrics/events on unknown channels are ignored—register channels via `MaudeOptions.AdditionalChannels`.
- Maude is currently targeted at Android/iOS with SkiaSharp-rendered visuals.
