# Maude — in-app performance tracker for .NET MAUI

Maude overlays live memory, FPS, and annotated events inside your MAUI app. Shake to open, annotate spikes, and keep context while you tune.

| <img src="https://github.com/matthewrdev/maude/blob/main/img/demo-animation.gif" alt="Shake gesture demo" style="max-height:200px; width:auto;"> | <img src="https://github.com/matthewrdev/maude/blob/main/img/demo-overlay.PNG" alt="Overlay demo" style="max-height:200px; width:auto;"> | <img src="https://github.com/matthewrdev/maude/blob/main/img/demo-slidesheet.jpeg" alt="Slide-sheet demo" style="max-height:200px; width:auto;"> |
| --- | --- | --- |
| **Shake to open Maude** | **Memory chart overlay** | **Slide-in events sheet** |

## Disclaimer ⚠️

Best effort has been made for performance and correctness, but Maude continuously snapshots memory and stores recent samples in-memory; expect a small observer effect.

*Please treat Maude’s numbers as a guidance, a heuristic.*

Always use the native tools and platform specific profilers (Xcode Instruments, Android Studio profiler) or `dotnet trace` for authoritative measurements.

## Quickstart

Install the package and hook Maude into your MAUI app startup.

### Setup

Android requires a window provider so Maude can attach its overlay to the current activity.

```csharp
// MauiProgram.cs
using Maude;

var maudeOptions = MaudeOptions.CreateBuilder()
  .WithMauiWindowProvider() // supplies the current Activity on Android
  .Build();

var builder = MauiApp.CreateBuilder()
  .UseMauiApp<App>()
  .UseMaudeAndActivate(maudeOptions); // or .UseMaude(maudeOptions) then MaudeRuntime.Activate()
```

### Present Maude

```csharp
MaudeRuntime.PresentSheet();   // Slide-in sheet
MaudeRuntime.PresentOverlay(); // Window overlay
MaudeRuntime.DismissOverlay();
```

## Documentation

Looking for builder options, event recording, FPS sampling, or platform-specific tips? Read the full guide at https://github.com/matthewrdev/maude/blob/main/docs.md.

## What does Maude capture?

Maude surfaces platform-native memory metrics and managed heap usage across Android, iOS, and Mac Catalyst. See the docs for details and references.

## Limitations and Known Issues

### Modal Pages

`WindowOverlay` attaches to the root window, so modal pages can obscure the overlay. Use the slide-in sheet (`PresentSheet`) for modal-heavy flows.

### Only Supported on .NET 9 and higher

Maude is explicitly built for .NET 9+ to leverage [`Span<T>` optimisations](https://learn.microsoft.com/en-us/dotnet/api/system.span-1?view=net-9.0) and [MAUI native embedding](https://learn.microsoft.com/en-us/dotnet/maui/whats-new/dotnet-9?view=net-maui-10.0&utm_source=chatgpt.com#native-embedding); earlier target frameworks are unsupported.

## More

Source, issues, and release notes live at https://github.com/matthewrdev/maude/.
