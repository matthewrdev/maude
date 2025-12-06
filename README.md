
# In-app observability for .NET MAUI.

[![Maude](https://img.shields.io/nuget/vpre/Maude.svg?cacheSeconds=3600&label=Maude%20nuget)](https://www.nuget.org/packages/Maude)

Maude is a plugin for .NET MAUI to monitor app memory at runtime and view it via live-rendered chart.

| <img src="https://github.com/matthewrdev/maude/blob/86d2f3f3ec478a815437966dcf0a79c949d11df4/img/demo-animation.gif" alt="Shake gesture demo" style="max-height:200px; width:auto;"> | <img src="https://github.com/matthewrdev/maude/blob/86d2f3f3ec478a815437966dcf0a79c949d11df4/img/demo-overlay.PNG" alt="Overlay demo" style="max-height:200px; width:auto;"> | <img src="https://github.com/matthewrdev/maude/blob/86d2f3f3ec478a815437966dcf0a79c949d11df4/img/demo-slidesheet.jpeg" alt="Slide-sheet demo" style="max-height:200px; width:auto;"> |
| --- | --- | --- |
| **Shake to open Maude** | **Memory chart overlay** | **Slide-in events sheet** |

## Disclaimer ⚠️

Best effort has been made for performance and correctness, but Maude continuously snapshots memory and stores recent samples in-memory; expect a small observer effect.

*Please treat Maude’s numbers as a guidance, a heuristic.*

Always use the native tools and platform specific profilers (Xcode Instruments, Android Studio profiler) or `dotnet trace` for authoritative measurements.

## Quickstart

Add Maude to your MAUI app with minimal code.

1) Configure the app builder:
```csharp
// MauiProgram.cs
using Maude;

// ...

var builder = MauiApp.CreateBuilder()
  .UseMauiApp<App>()
  .UseMaude();  
```

1) Start tracking memory usage:
```csharp
    MaudeRuntime.Activate();
```

1) Show Maude:
```csharp
// Show Maude as a slide in sheet.
MaudeRuntime.PresentSheet();   // Open the chart and events view as a slide in.
MaudeRuntime.DismissSheet();   // Close the slide in sheet.

// Show Maude as a window overlay.
MaudeRuntime.PresentOverlay();   // Show the chart as a window overlay.
MaudeRuntime.DismissOverlay();   // Close the overlay.
```

Or, if you would prefer a one-liner, add the following to your MAUI app builder:

```csharp
// MauiProgram.cs
using Maude;

// ...

var builder = MauiApp.CreateBuilder()
  .UseMauiApp<App>()
  // Register Maude and immediately start tracking.
  .UseMaudeAndActivate();  
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

## FPS Tracking

Maude can sample frames-per-second alongside memory metrics and overlay the results on the chart. Enable FPS capture when building options:

```csharp
var options = MaudeOptions.CreateBuilder()
    .WithFramesPerSecond()
    .Build();
```

At runtime you can call `MaudeRuntime.EnableFramesPerSecond()` or `.DisableFramesPerSecond()` to toggle sampling without rebuilding the options. FPS series segments automatically change color as the rate crosses the built-in thresholds (Optimal ≥50, Stable 40–49, Fair 30–39, Poor 20–29, Critical <20) so jank is easy to spot.

## What does Maude capture?

### Android

| Metric | Description + Documentation |
|--------|-----------------------------|
| **Resident Set Size (RSS)** | Physical RAM currently mapped into the process (Java + native + runtime), excluding swapped pages. [Android Memory Overview](https://developer.android.com/topic/performance/memory-overview#mem-anatomy) • [`/proc` reference](https://man7.org/linux/man-pages/man5/proc.5.html) |
| **Native Heap** | Memory allocated through native allocators (`malloc`, `new`) used by the ART runtime and native libraries. [`Debug.getNativeHeapAllocatedSize`](https://developer.android.com/reference/android/os/Debug#getNativeHeapAllocatedSize) |
| **CLR (Managed Heap)** | Managed heap consumed by the .NET/Mono runtime (GC generations, LOH, objects, metadata). [.NET GC Fundamentals](https://learn.microsoft.com/dotnet/standard/garbage-collection/fundamentals) |

### iOS

| Metric | Description + Documentation |
|--------|-----------------------------|
| **Physical Footprint (Jetsam Footprint)** | Total physical RAM attributed to the process by the kernel — the metric Jetsam uses to terminate apps. [`task_vm_info_data_t`](https://developer.apple.com/documentation/kernel/task_vm_info_data_t) • [WWDC Memory Deep Dive](https://developer.apple.com/videos/play/wwdc2018/416/) |
| **CLR (Managed Heap)** | Managed memory used by the .NET/Mono runtime on iOS (AOT GC heap + metadata). [.NET GC Fundamentals](https://learn.microsoft.com/dotnet/standard/garbage-collection/fundamentals) |


## Limitations and Known Issues

### Modal Pages

MAUI’s `WindowOverlay` attaches to the root window, so modal pages can obscure the overlay. Use the slide-in sheet (`PresentSheet`) for modal-heavy flows. 

On Android, the overlay is a transparent `FrameLayout` added to the current activity’s decor view; it stays on top of your main content but under system UI and will not be visible on modal pages. 

On iOS, a non-interactive `UIView` is injected into every active `UIWindow` (per scene); the overlay follows window bounds but will sit behind any OS-owned alerts or modal views.

### Only Supported on .NET 9 and higher

Maude is explicitly built for .NET 9+ to leverage [`Span<T>` optimisations](https://learn.microsoft.com/en-us/dotnet/api/system.span-1?view=net-9.0), which enables some performance oriented code in the chart rendering, and [MAUI native embedding](https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-9?view=net-maui-10.0&utm_source=chatgpt.com#native-embedding), which enables Maude's UIs to be built in MAUI but rendered inside native views.

As such, target frameworks earlier than .NET 9 are unsupported.

## Design Goals

**Minimal External Dependencies**

Maude *must not* add undue dependencies to the integrating application.

As much as possible, Maude must use the core MAUI and platform APIs. Maudes only current external dependency is SkiaSharp.

**Minimal Overhead**

Maude *should not* impact the performance of the integrating application.

Maude should capture and present telemetry in the most efficient method possible and ensure it adds minimal memory overhead.

**Simple Integration**

Maude *must* be simple for the integrating application to add and use.

Currently, Maude can be added to an applicaton in one line `.UseMaudeAndActivate()`.
