
# Maude - In-app observability for .NET MAUI.

![Maude logo](https://github.com/matthewrdev/maude/blob/5e22911afbc5c6eef5cf839b9bc91b79b3522c8b/img/maude_small.png)

```
Maude (Name, Germanic): Mighty in battle, powerful battler.
```

Maude monitors your apps memory and displays it via an in-app, live-rendered chart.

Maude, aka Maui-Debug, is a powerful, lightweight tool to help in your debugging battles.

## Disclaimer

Best effort has been made for performance and correctness, but Maude continuously snapshots memory and stores recent samples in-memory; expect a small observer effect.

Treat Maude’s numbers as guidance; use platform profilers (Xcode Instruments, Android Studio profiler) or `dotnet trace` for authoritative measurements.

## Quickstart

Install Maude

Add Maude to your MAUI app with minimal code.

1) Configure the app builder:
```csharp
// MauiProgram.cs
using Maude;

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder()
        .UseMauiApp<App>()
        .UseMaude<App>();  // Initialises and registers the Maude runtime, configures required fonts + adds SkiaSharp.
    return builder.Build();
}
```
2) Show Maude:
```csharp

// Show Maude as a slide in sheet.
MaudeRuntime.PresentSheet();   // Open the sheet
MaudeRuntime.DismissSheet();   // Close it.

// Show Maude as a window overlay.
MaudeRuntime.PresentOverlay();   // overlay pinned to a window corner
MaudeRuntime.DismissOverlay();   // close it
```

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
MaudeRuntime.Event("Cache cleared", 96);                    // default icon
MaudeRuntime.Event("GC requested", MaterialSymbols.Delete,  // custom icon
                   MaudeConstants.ReservedChannels.ChannelNotSpecified_Id);
MaudeRuntime.Event("Large download", "cloud_download", 97, "42 MB");
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
    .WithShakeGestureBehaviour(MaudeShakeGestureBehaviour.Overlay) // or SlideSheet
    .WithAdditionalLogger(new MyLogger())     // or .WithBuiltInLogger()
    .Build();
```

### Platform Initialisation

While the MauiAppBuilder extension registers and initialises Maude, it may be desireable to ensure that Maude is sampling immediately when you're app starts.

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

If you prefer dependency injection, use `builder.UseMaude<App>()` in `MauiProgram` which registers the runtime and fonts; call `MaudeRuntime.Initialize`/`Activate` later when you want to start sampling.

## Notes

- Reserved channel IDs: `0` (.NET/CLR), `1` (platform), `255` (not specified); use other IDs for custom channels.
- Metrics/events on unknown channels are ignored—register channels via `MaudeOptions.AdditionalChannels`.
- Maude is currently targeted at Android/iOS with SkiaSharp-rendered visuals.

## Limitations and Known Issues

- Modal pages: MAUI’s `WindowOverlay` attaches to the root window, so modal pages can obscure the overlay. Use the slide-in sheet (`Present`) for modal-heavy flows.
- Overlay overhead: the overlay is Skia-rendered and re-blitted while visible; expect a small temporary memory bump from the render target/frame buffer.
- Target framework: built for .NET 9+ to leverage `Span<T>` optimisations and MAUI native embedding; earlier TFMs are unsupported.

native embedding: https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-9?view=net-maui-10.0&utm_source=chatgpt.com#native-embedding
Spans: https://learn.microsoft.com/en-us/dotnet/api/system.span-1?view=net-9.0

