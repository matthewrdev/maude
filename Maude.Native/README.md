# Maude.Native — in-app performance tracker for .NET iOS, Android, and Mac Catalyst

Maude overlays live memory, FPS, and annotated events inside your app. It works with .NET for iOS, Android, and Mac Catalyst. Shake to open, annotate spikes, and keep context while you tune.

| <img src="https://github.com/matthewrdev/maude/blob/main/img/demo-animation.gif" alt="Shake gesture demo" style="max-height:200px; width:auto;"> | <img src="https://github.com/matthewrdev/maude/blob/main/img/demo-overlay.PNG" alt="Overlay demo" style="max-height:200px; width:auto;"> | <img src="https://github.com/matthewrdev/maude/blob/main/img/demo-slidesheet.jpeg" alt="Slide-sheet demo" style="max-height:200px; width:auto;"> |
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

## Documentation

Looking for builder options, event recording, FPS sampling, or platform-specific tips? Read the full guide at https://github.com/matthewrdev/maude/blob/main/docs.md.

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

### Only Supported on .NET 9 and higher

Maude is explicitly built for .NET 9+ to leverage [`Span<T>` optimisations](https://learn.microsoft.com/en-us/dotnet/api/system.span-1?view=net-9.0); earlier target frameworks are unsupported.

## More

Source, issues, and release notes live at https://github.com/matthewrdev/maude.
