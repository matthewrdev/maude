
# Maude - In-app observability for .NET MAUI.

Maude is a plugin for .NET MAUI to monitor app memory at runtime and view it via live-rendered chart.

Maude, aka Maui-Debug, is a powerful, lightweight tool to help you in your debugging battles.

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

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder()
        .UseMauiApp<App>()
        // Initialises and registers the Maude runtime, configures required fonts + adds SkiaSharp. 
        // Optionally provide 'MaudeOptions' here to customise.
        .UseMaude();  
    return builder.Build();
}
```

2) Start tracking memory usage:
```csharp
    MaudeRuntime.Activate();
```


3) Show Maude:
```csharp
// Show Maude as a slide in sheet.
MaudeRuntime.PresentSheet();   // Open the chart and events view as a slide in.
MaudeRuntime.DismissSheet();   // Close the slide in sheet.

// Show Maude as a window overlay.
MaudeRuntime.PresentOverlay();   // Show the chart as a window overlay.
MaudeRuntime.DismissOverlay();   // Close the overlay.
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
    .WithDefaultOverlayPosition(MaudeOverlayPosition.TopRight) // default anchor when showing overlay without an explicit position
    .WithShakeGestureBehaviour(MaudeShakeGestureBehaviour.Overlay) // or SlideSheet
    .WithAdditionalLogger(new MyLogger())     // or .WithBuiltInLogger()
    .Build();
```

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
| **Available Headroom** | Approximate remaining memory the process can consume before triggering Jetsam pressure. [`os_proc_available_memory` source](https://github.com/apple-oss-distributions/libmalloc/blob/main/libmalloc/os_alloc_once_private.h) |
| **CLR (Managed Heap)** | Managed memory used by the .NET/Mono runtime on iOS (AOT GC heap + metadata). [.NET GC Fundamentals](https://learn.microsoft.com/dotnet/standard/garbage-collection/fundamentals) |


## Limitations and Known Issues

### Modal Pages

MAUI’s `WindowOverlay` attaches to the root window, so modal pages can obscure the overlay. Use the slide-in sheet (`PresentSheet`) for modal-heavy flows. 

On Android, the overlay is a transparent `FrameLayout` added to the current activity’s decor view; it stays on top of your main content but under system UI and will not be visible on modal pages. 

On iOS, a non-interactive `UIView` is injected into every active `UIWindow` (per scene); the overlay follows window bounds but will sit behind any OS-owned alerts or modal views.

### Only Supported on .NET 9 and higher

Maude is explicitly built for .NET 9+ to leverage [`Span<T>` optimisations](https://learn.microsoft.com/en-us/dotnet/api/system.span-1?view=net-9.0), which enables some performance oriented code in the chart rendering, and [MAUI native embedding](https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-9?view=net-maui-10.0&utm_source=chatgpt.com#native-embedding), which enables Maude's UIs to be built in MAUI but rendered inside native views.

As such, target frameworks earlier than .NET 9 are unsupported.
