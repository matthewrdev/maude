# Maude Native Port Report

## Overview
- Goal: map Maude’s .NET-native HUD (memory/FPS chart, events, overlay/sheet) to native Swift (iOS/macOS), Java/Kotlin (Android), Flutter, and React Native with equivalent APIs and UX.
- Core components to replicate: data sink with retention, timers for sampling, FPS monitor, Skia-based chart renderer with probe/events, overlay + sheet hosts, builder-style runtime options, and snapshot/export hooks.
- Guiding principles: keep sampling/rendering native to minimize overhead; mirror current option surface; conditionally expose framework-specific channels (Dart/JS heaps) only when available; preserve overlay pass-through vs inline interactivity.

## Platform Summaries
- Swift (iOS/macOS): Swift Package/XCFramework with native sampler (task_vm_info), CADisplayLink FPS, Skia/CoreGraphics renderer, UIView/NSView chart + overlay + sheet VC; optional shake gesture and snapshot action.
- Java/Kotlin (Android): AAR with Activity-driven overlay/sheet, Skia/Canvas chart view, `/proc/self/status` VmRSS + NativeHeap + Java heap samplers, Choreographer FPS, shake detector; Compose wrapper optional.
- Flutter: Flutter plugin exposing PlatformView chart and native overlay/sheet; native sampling/rendering reused; optional Dart heap channel via VM service (debug/profile only); FPS from native vsync or Dart FrameTiming.
- React Native: Native module + Fabric view for chart; native sampling/rendering; JS engine heap channel (Hermes/V8) when available; FPS from native UI thread plus optional JS-thread sampler.

## Platform Deep Dives
### Swift (iOS/macOS)
- Metrics: physical/jetsam via task_vm_info; managed/CLR channel drops; consider optional “App heap” from malloc introspection if feasible.
- FPS: CADisplayLink sampling (~0.5s windows).
- UI: UIView/NSView chart (SkiaKit or CoreGraphics reimplement), sheet controller with overlay toggle + snapshot, overlay view anchored to window positions.
- Gestures: pan probe + tap to clear; shake via motion events/CMMotionManager with predicate.
- API: builder mirroring MaudeOptions; runtime with present/dismiss sheet/overlay, event/metric, theme/event rendering toggles, snapshot callback.

### Java/Kotlin (Android)
- Metrics: PSS via ActivityManager, RSS via `/proc/self/status` VmRSS, Java heap via Runtime, Native heap via Debug.getNativeHeapAllocatedSize. Managed/CLR channel drops.
- FPS: Choreographer frame callback with rolling window (~0.5s).
- UI: BottomSheetDialog host with chart + RecyclerView events; overlay FrameLayout on decor with gravity; Compose wrapper optional.
- Gestures: touch probe in inline; overlay non-interactive; shake detector with predicate.
- API: Kotlin builder/runtime mirroring .NET (options, present/dismiss, events/metrics, FPS/theme/event rendering, snapshot).

### Flutter
- Data path: keep sink/sampling native; Dart acts as control plane via MethodChannel. Optional Dart heap channel using VM service (debug/profile only); hide in release.
- Rendering: native chart exposed as PlatformView for inline; native overlay/sheet invoked via channel.
- FPS: native monitor; optionally feed Dart FrameTiming (Window.onReportTimings) to a separate channel for build/raster FPS.
- API: Dart facade with builder that maps to native options; commands for present/dismiss, events/metrics, FPS, theme, event rendering, snapshot.
- Samples: Flutter example showing inline widget + overlay toggle.

### React Native
- Data path: keep sink/sampling native; JS bridge for commands; Fabric component for inline chart.
- JS engine heap: Hermes via getHeapInfo; V8 via GetHeapStatistics; expose as JsHeap_* channel when present.
- FPS: native UI-thread FPS; optional JS-thread FPS via RAFTick/setImmediate counting.
- API: JS module mirroring options; imperative calls for present/dismiss/metrics/events/FPS/theme/event rendering/snapshot; Fabric prop-based chart for inline use.
- Samples: RN example with inline chart, overlay toggle, event/metric buttons.

## Conclusions
- Native-first rendering and sampling should be preserved across all targets to keep overhead low and behavior consistent.
- Framework-specific heaps (Dart/JS) are conditional: expose when supported (debug/profile or engine APIs) and hide otherwise to avoid noisy channels.
- Existing Maude option surface can be mirrored with minimal divergence; key gaps are managed/CLR metrics (not applicable) and platform-specific heap channels.
- FPS: platform vsync monitors remain valid; add optional Dart/JS thread FPS for deeper insight where feasible.
- Deliverables should include per-platform sample apps and clear docs on which channels appear in which build modes and engines.***
