# .NET-native in-app performance tracker for iOS, Android, Mac Catalyst, and MAUI.

[![Maude](https://img.shields.io/nuget/vpre/Maude.svg?cacheSeconds=3600&label=Maude%20nuget)](https://www.nuget.org/packages/Maude)

Maude is a .NET-native, integrated performance tracker that overlays live memory, FPS, and annotated events inside your app. It works with .NET for iOS, Android, Mac Catalyst, and can be hosted inside .NET MAUI via native embedding.

| <img src="https://github.com/matthewrdev/maude/blob/86d2f3f3ec478a815437966dcf0a79c949d11df4/img/demo-animation.gif" alt="Shake gesture demo" style="max-height:200px; width:auto;"> | <img src="https://github.com/matthewrdev/maude/blob/86d2f3f3ec478a815437966dcf0a79c949d11df4/img/demo-overlay.PNG" alt="Overlay demo" style="max-height:200px; width:auto;"> | <img src="https://github.com/matthewrdev/maude/blob/86d2f3f3ec478a815437966dcf0a79c949d11df4/img/demo-slidesheet.jpeg" alt="Slide-sheet demo" style="max-height:200px; width:auto;"> |
| --- | --- | --- |
| **Shake to open Maude** | **Memory chart overlay** | **Slide-in events sheet** |

## Disclaimer ⚠️

Best effort has been made for performance and correctness, but Maude continuously snapshots memory and stores recent samples in-memory; expect a small observer effect.

*Please treat Maude’s numbers as a guidance, a heuristic.*

Always use the native tools and platform specific profilers (Xcode Instruments, Android Studio profiler) or `dotnet trace` for authoritative measurements.

## Quickstart

Pick the host style that suits your app.

### .NET for iOS, Android, and Mac Catalyst

1) Provide a presentation window (Android requires an `Activity`):
```csharp
// Android Activity
var options = MaudeOptions.CreateBuilder()
    .WithPresentationWindowProvider(() => this) // required on Android
    .Build();

MaudeRuntime.InitializeAndActivate(options);
```

On iOS or Mac Catalyst, the default window provider is used:
```csharp
MaudeRuntime.InitializeAndActivate();
```

2) Present Maude in your UI:
```csharp
MaudeRuntime.PresentSheet();   // Slide-in sheet
MaudeRuntime.PresentOverlay(); // Window overlay
MaudeRuntime.DismissOverlay();
```

### .NET MAUI host

MAUI on Android must supply a delegate that returns the current activity so Maude can attach its overlay.

1) Configure the app builder:
```csharp
// MauiProgram.cs
using Maude;

var maudeOptions = MaudeOptions.CreateBuilder()
  .WithMauiWindowProvider() // supplies the current Activity on Android
  .Build();

var builder = MauiApp.CreateBuilder()
  .UseMauiApp<App>()
  .UseMaude(maudeOptions);
```

2) Start tracking memory usage:
```csharp
MaudeRuntime.Activate();
```

3) Show Maude:
```csharp
// Show Maude as a slide in sheet.
MaudeRuntime.PresentSheet(); 
MaudeRuntime.DismissSheet(); 

// Show Maude as a window overlay.
MaudeRuntime.PresentOverlay();
MaudeRuntime.DismissOverlay();
```

If you would prefer a one-liner, add the following to your MAUI app builder:

```csharp
// MauiProgram.cs
using Maude;

var maudeOptions = MaudeOptions.CreateBuilder()
  .WithMauiWindowProvider() // required on Android
  .Build();

var builder = MauiApp.CreateBuilder()
  .UseMauiApp<App>()
  .UseMaudeAndActivate(maudeOptions);  // Register Maude and immediately start tracking.
```

## [Documentation](docs.md) 

Looking for integration steps, builder options, and runtime usage guidance beyond the quickstart? 

See [docs.md](docs.md) for the full walkthrough, including event recording, customisation options, platform-specific initialisation (including the MAUI Android activity delegate), and FPS sampling tips.

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

When hosted inside MAUI, `WindowOverlay` attaches to the root window, so modal pages can obscure the overlay. Use the slide-in sheet (`PresentSheet`) for modal-heavy flows. 

On Android, the overlay is a transparent `FrameLayout` added to the current activity’s decor view; it stays on top of your main content but under system UI and will not be visible on modal pages. 

On iOS, a non-interactive `UIView` is injected into every active `UIWindow` (per scene); the overlay follows window bounds but will sit behind any OS-owned alerts or modal views.

### Only Supported on .NET 9 and higher

Maude is explicitly built for .NET 9+ to leverage [`Span<T>` optimisations](https://learn.microsoft.com/en-us/dotnet/api/system.span-1?view=net-9.0), which enables some performance oriented code in the chart rendering, and [MAUI native embedding](https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-9?view=net-maui-10.0&utm_source=chatgpt.com#native-embedding), which enables Maude's UIs to be built in MAUI but rendered inside native views.

As such, target frameworks earlier than .NET 9 are unsupported.

## Design Goals

**Minimal External Dependencies**

Maude *must not* add undue dependencies to the integrating application.

As much as possible, Maude must use the core .NET and platform APIs. Maude’s only current external dependency is SkiaSharp.

**Minimal Overhead**

Maude *should not* impact the performance of the integrating application.

Maude should capture and present telemetry in the most efficient method possible and ensure it adds minimal memory overhead.

**Simple Integration**

Maude *must* be simple for the integrating application to add and use across .NET for iOS, Android, Mac Catalyst, and MAUI.

Currently, Maude can be added to an application in one line `.UseMaudeAndActivate()`.
