# Maude Usage and Builder Guide

This document provides the detailed integration and configuration guidance referenced by the main `README.md`.

## Record Events

Record markers and additional metrics so memory spikes have context.

```csharp
// Define channels first (avoid reserved IDs 0, 1 and 255).
var channels = new []
{
    new MaudeChannel(96, "Image Cache", Colors.Orange),
    new MaudeChannel(97, "Network Buffers", Colors.Green)
};

// Initialise Maude with those channels.
var options = MaudeOptions.CreateBuilder()
    .WithAdditionalChannels(channels)
    .Build();
MaudeRuntime.InitializeAndActivate(options);

// Add metrics (rendered as extra series).
MaudeRuntime.Metric(currentCacheSizeBytes, 96);

// Add events (rendered as vertical markers + items in the event list).
MaudeRuntime.Event("Cache cleared", 96);                    // default type + icon "*"
MaudeRuntime.Event("GC requested", MaudeEventType.Gc);      // GC event symbol "g"
MaudeRuntime.Event("Large download", MaudeEventType.Event, 97, "42 MB");
```

Events/metrics on unknown channels are ignored. Both the slide-in sheet and overlay display the channels and event markers, letting you correlate spikes with the moments you annotated.

## Customize Maude

Use the `MaudeOptionsBuilder` to tune sampling, channels, gestures and logging:

```csharp
var options = MaudeOptions.CreateBuilder()
    .WithSampleFrequencyMilliseconds(500)     // clamp: 200–2000 ms
    .WithRetentionPeriodSeconds(10 * 60)      // clamp: 60–3600 s
    .WithAdditionalChannels(customChannels)   // extra metric/event series
    .WithShakeGesture()                       // enable shake-to-toggle
    .WithDefaultOverlayPosition(MaudeOverlayPosition.TopRight) // default anchor when showing overlay without an explicit position
    .WithShakeGestureBehaviour(MaudeShakeGestureBehaviour.Overlay) // or SlideSheet
    .WithEventRenderingBehaviour(MaudeEventRenderingBehaviour.IconsOnly) // LabelsAndIcons, IconsOnly (default), None
    .WithAdditionalLogger(new MyLogger())     // or .WithBuiltInLogger()
    .Build();
```

Use `WithEventRenderingBehaviour` (or change `MaudeRuntime.EventRenderingBehaviour` at runtime) to choose between icons with labels, icons only, or hiding events entirely. This applies to both the slide sheet and overlay chart.

### Control the Default Memory Metrics

`MaudeOptions` now exposes `WithDefaultMemoryChannels` and `WithoutDefaultMemoryChannels` so you can decide which of Maude's built-in metrics appear. The flags in `MaudeDefaultMemoryChannels` cover the CLR managed heap, Android's native heap, Android's RSS, and iOS's physical footprint channel.

```csharp
var options = MaudeOptions.CreateBuilder()
    .WithDefaultMemoryChannels(MaudeDefaultMemoryChannels.ManagedHeap) // managed heap only
    .Build();

var hideNoise = MaudeOptions.CreateBuilder()
    .WithoutDefaultMemoryChannels(MaudeDefaultMemoryChannels.NativeHeap | MaudeDefaultMemoryChannels.ResidentSetSize)
    .Build();
```

By default Maude enables the platform-appropriate channels (CLR + Native + RSS on Android, CLR + Physical Footprint on iOS). Supplying `MaudeDefaultMemoryChannels.None` hides every built-in memory series so you can overlay only your custom channels.

### On-demand Shake Predicate

When you need to gate the shake gesture behind your own runtime configuration, provide a predicate. Maude consults it before activating the listener and each time a shake occurs, so you don't have to manually call `EnableShakeGesture` or `DisableShakeGesture` as your config changes.

```csharp
var options = MaudeOptions.CreateBuilder()
    .WithShakeGesture()
    .WithShakeGesturePredicate(() => MyDebugConfig.IsShakeAllowed)
    .Build();
```

If the predicate returns `false`, the accelerometer remains registered but shakes are ignored until the predicate later returns `true`. If it throws, Maude logs the exception and suppresses the shake.

### Platform initialisation

While the `MauiAppBuilder` extension registers and initialises Maude, it may be desireable to ensure that Maude is sampling immediately when your app starts.

To do so:

- **Android**: initialise inside `MainApplication` so the runtime is ready before `CreateMauiApp()`:

```csharp
public MainApplication(IntPtr handle, JniHandleOwnership ownership)
    : base(handle, ownership)
{
    var options = /* build options as above */;
    MaudeRuntime.InitializeAndActivate(options);
}
```

- **iOS/macOS Catalyst**: initialise before `UIApplication.Main` in `Program.cs`:

```csharp
var options = /* build options */;
MaudeRuntime.InitializeAndActivate(options);
UIApplication.Main(args, null, typeof(AppDelegate));
```

## FPS Tracking

Maude can sample frames-per-second alongside memory metrics and overlay the results on the chart. Enable FPS capture when building options:

```csharp
var options = MaudeOptions.CreateBuilder()
    .WithFramesPerSecond()
    .Build();
```

At runtime you can call `MaudeRuntime.EnableFramesPerSecond()` or `.DisableFramesPerSecond()` to toggle sampling without rebuilding the options. FPS series segments automatically change color as the rate crosses the built-in thresholds (Optimal ≥50, Stable 40–49, Fair 30–39, Poor 20–29, Critical <20) so jank is easy to spot.
