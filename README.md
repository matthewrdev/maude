
# Maude - In-app observability for .NET MAUI.

Maude is a plugin for .NET MAUI that monitors app memory and displays it via an in-app, live-rendered chart.

| <img src="https://github.com/matthewrdev/maude/blob/86d2f3f3ec478a815437966dcf0a79c949d11df4/img/demo-animation.gif" alt="Shake gesture demo" style="max-height:200px; width:auto;"> | <img src="https://github.com/matthewrdev/maude/blob/86d2f3f3ec478a815437966dcf0a79c949d11df4/img/demo-overlay.PNG" alt="Overlay demo" style="max-height:200px; width:auto;"> | <img src="https://github.com/matthewrdev/maude/blob/86d2f3f3ec478a815437966dcf0a79c949d11df4/img/demo-slidesheet.jpeg" alt="Slide-sheet demo" style="max-height:200px; width:auto;"> |
| --- | --- | --- |
| **Shake to open Maude** | **Memory chart overlay** | **Slide-in events sheet** |

Maude, aka Maui-Debug, is a powerful, lightweight tool to help you in your debugging battles.

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
        .UseMaude<App>();  // Initialises and registers the Maude runtime, configures required fonts + adds SkiaSharp.
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


| Platform | Metric | Description + Documentation |
|---------|--------|-----------------------------|
| **Android** | **Resident Set Size (RSS)** | Physical RAM currently mapped into the process (Java + native + runtime), excluding swapped pages. [Android Memory Overview](https://developer.android.com/topic/performance/memory-overview#mem-anatomy) • [`/proc` reference](https://man7.org/linux/man-pages/man5/proc.5.html) |
| **Android** | **Native Heap** | Memory allocated through native allocators (`malloc`, `new`) used by native libraries and the ART runtime. [`Debug.getNativeHeapAllocatedSize`](https://developer.android.com/reference/android/os/Debug#getNativeHeapAllocatedSize) |
| **Android** | **CLR (Managed Heap)** | Managed heap consumed by the .NET/Mono runtime (GC generations, LOH, objects, metadata). [.NET GC Fundamentals](https://learn.microsoft.com/dotnet/standard/garbage-collection/fundamentals) |
| **iOS** | **Physical Footprint (Jetsam Footprint)** | Total physical RAM attributed to the process by the kernel — the value Jetsam uses to kill apps. [`task_vm_info_data_t`](https://developer.apple.com/documentation/kernel/task_vm_info_data_t) • [WWDC Memory Deep Dive](https://developer.apple.com/videos/play/wwdc2018/416/) |
| **iOS** | **Available Headroom** | Approximate remaining memory the process can consume before hitting Jetsam pressure. [`os_proc_available_memory` source](https://github.com/apple-oss-distributions/libmalloc/blob/main/libmalloc/os_alloc_once_private.h) |
| **iOS** | **CLR (Managed Heap)** | Managed memory consumed by the .NET/Mono runtime on iOS (AOT GC heap + metadata). [.NET GC Fundamentals](https://learn.microsoft.com/dotnet/standard/garbage-collection/fundamentals) |



## Limitations and Known Issues

### Modal Pages

MAUI’s `WindowOverlay` attaches to the root window, so modal pages can obscure the overlay. Use the slide-in sheet (`Present`) for modal-heavy flows.

### Target framework

Maude is explicitly built for .NET 9+ to leverage [`Span<T>` optimisations](https://learn.microsoft.com/en-us/dotnet/api/system.span-1?view=net-9.0) and [MAUI native embedding](https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-9?view=net-maui-10.0&utm_source=chatgpt.com#native-embedding); earlier target frameworks are unsupported.
