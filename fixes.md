# Artisense Optimization Fixes

This document outlines recommended changes to enhance the performance of the Artisense application, focusing on meeting the MVP requirements for latency (≤15ms p50, ≤20ms p95), CPU usage (<1% idle, <5% active), memory footprint (<30MB), and overall efficiency. These suggestions are based on a review of the current codebase without making any modifications. Changes are categorized by component and prioritize low-latency audio playback driven by global pen input.

The proposed optimizations adhere to SOLID, DRY, KISS, and YAGNI principles, targeting C# 10 conventions with nullable reference types. Estimated impact on benchmarks (using BenchmarkDotNet) is noted where applicable. All changes would require updating unit/integration tests (≥90% coverage) and running CI/CD pipelines to verify no regressions.

## 1. InputService Optimizations (GlobalHookPenProvider.cs and PenInputService.cs)

The InputService handles global pen detection, which is the first stage in the end-to-end latency pipeline. Current implementation uses a low-level mouse hook, which is efficient but incurs overhead from extensive per-event processing. Key issues: Synchronous heavy checks in the hot path (HookCallback) and logging, leading to potential >5ms added latency per event.

### F1.1: Simplify Terminal Window Detection
- **Current Issue**: `IsTerminalWindow` calls `GetWindowUnderCursor`, `GetClassName`, `GetWindowText`, and `GetProcessNameFromWindow` on every mouse event. This involves multiple P/Invoke calls and string operations, adding ~2-5ms latency and ~1-2% CPU overhead during active drawing (violates P2.2).
- **Proposed Change**: Cache window class/process checks or skip for non-contact events. Implement a faster heuristic using only `GetClassName` and a smaller whitelist of known terminal classes (e.g., limit to "ConsoleWindowClass", "tty"). Move full title/process checks to a background thread or only on PenDown.
- **Impact**: Reduces per-event latency by 2-3ms; CPU savings of 0.5-1% during drawing. Meets P1.1/P1.2 more reliably.
- **Trade-offs**: Slight risk of false negatives for rare terminals; mitigate with logging in debug mode only.
- **Test**: Add benchmark for hook callback execution time; ensure F1.1 global detection works in Photoshop/Krita.

### F1.2: Optimize Pen Detection (IsPenMessage)
- **Current Issue**: Multiple bitwise checks and console logging per event. Permissive logic (treat non-zero extraInfo as pen) may cause false positives, triggering unnecessary audio starts (extra CPU).
- **Proposed Change**: Pre-compile signatures into a HashSet<long> for O(1) lookup, prioritizing the WACOM_SIGNATURE (0xFF515700) for Wacom Intuos Pro S compatibility. Remove console logging from hot path (use ILogger.Debug only). Add pressure validation post-detection to filter false positives (e.g., if pressure == 0.0f and not expected, ignore).
- **Impact**: 0.5-1ms latency reduction; <0.5% CPU drop. Improves accuracy for F1.2 pressure normalization, especially for Wacom Intuos Pro S tablets.
- **Trade-offs**: None significant; aligns with F1.5 fallback to Wintab.
- **Test**: Integration test with simulated extraInfo, including Wacom Intuos Pro S signature; benchmark detection time.

### F1.3: Reduce Logging in Event Handlers (PenInputService)
- **Current Issue**: `OnPenDown`/`OnPenMove` use `LogDebug` synchronously, adding minor latency (~0.1ms) and I/O overhead.
- **Proposed Change**: Make logging async (e.g., `LogDebugAsync`) or conditional (e.g., if pressure changes >0.01f). In production, disable debug logging entirely via appsettings.
- **Impact**: Negligible latency improvement but cumulative CPU savings for high-frequency moves.
- **Trade-offs**: Debugging slightly harder; use structured logging for analysis.
- **Test**: Ensure events forward correctly (F1.3 PenDown/Move/Up).

## 2. AudioService Optimizations (WasapiAudioService.cs and PressureVolumeProcessor.cs)

Audio playback is the critical path for latency. Current use of WASAPI exclusive mode with NAudio is appropriate, but buffer sizing, processing loops, and resource management can be tuned for lower latency/CPU.

### F2.1: Tune WASAPI Buffer Size
- **Current Issue**: Fixed 50ms buffer in exclusive mode (100ms fallback). This meets P1 but leaves headroom; larger buffers increase latency variability.
- **Proposed Change**: Dynamically set buffer to 10-20ms based on hardware (use `AudioClient.GetMixFormat` to query min buffer duration). Fallback to shared only if exclusive fails. Ensure loop stream reads minimal data per callback.
- **Impact**: Reduces p95 latency to ≤18ms; aligns with P1.2. CPU <3% active via efficient buffering.
- **Trade-offs**: Risk of underruns on low-end hardware; add retry logic on init failure.
- **Test**: Latency benchmark (WASAPI loopback, ±2ms tolerance per P1.3); verify F2.4 seamless looping.

### F2.2: Optimize Volume Processing Loop
- **Current Issue**: `Read` method in PressureVolumeProcessor loops over all samples with a simple low-pass filter. For 44.1kHz, this is ~441 iterations per 10ms, consuming ~1-2% CPU. No SIMD/vectorization.
- **Proposed Change**: Vectorize the loop using System.Numerics (e.g., process 4 floats at once with Vector4). Pre-compute alpha constant. Apply envelope only on pressure changes to skip full reprocessing.
- **Impact**: CPU reduction to <2% active; negligible latency impact but smoother performance (P2.2).
- **Trade-offs**: Requires .NET 6+ vector support; test on ARM/x86.
- **Test**: BenchmarkDotNet for Read method; ensure F2.2 perceptual curve (pressure^0.6) and F2.3 10ms envelope.

### F2.3: Enhance Idle Resource Disposal
- **Current Issue**: 5s idle timer disposes resources, but during short pauses, audio may reinitialize frequently (adding 50-100ms startup latency).
- **Proposed Change**: Use a shorter timer (1s) for disposal but cache initialized components (e.g., keep WaveFileReader loaded). Implement warm-up on PenDown prediction if possible.
- **Impact**: Startup latency <50ms (aids P4.1 <300ms overall); memory stable <25MB (P3.1).
- **Trade-offs**: Slightly higher idle memory; profile with dotMemory for undisposed handles (P3.2).
- **Test**: Memory leak test over 24h (P3.3); F2.5 immediate stop on PenUp.

## 3. CoreController Optimizations (ArtisenseController.cs)

The controller integrates services with direct event forwarding, which is low-overhead. No major issues, but ensure thread safety and minimal processing.

### F3.1: Async Event Handling
- **Current Issue**: Synchronous forwarding from Input to Audio; if Audio init blocks, it delays events.
- **Proposed Change**: Use async/await for Start/SetPressure calls (e.g., `await Task.Run(() => audioService.Start(pressure))` if needed). Add SynchronizationContext to avoid UI thread blocking.
- **Impact**: Prevents latency spikes >15ms during init; CPU balanced.
- **Trade-offs**: Minor complexity; ensure no race conditions in IsPlaying.
- **Test**: End-to-end latency benchmark; Q2.2 device reconnection.

## 4. Memory and Resource Management Improvements

### P3.1: Reduce Footprint
- **Current Issue**: Embedded audio stream and buffers (e.g., smoothingBuffer at 441 samples) contribute to ~20MB; multiple providers loaded.
- **Proposed Change**: Load providers lazily (only start one active per PenInputService). Use smaller buffers (e.g., 220 samples for 5ms smoothing). Compress audio asset if possible.
- **Impact**: Peak memory <25MB; zero leaks via explicit Dispose calls.
- **Trade-offs**: None; aligns with D1.1 single exe <6MB.
- **Test**: dotMemory snapshot for undisposed handles; 24h runtime test.

### P3.2: Global Resource Cleanup
- **Current Issue**: TrayManager disposes controller but not all services; potential leaks on app switch/exit.
- **Proposed Change**: Implement IHostedService in controller for full lifecycle management. Use WeakEvents for subscriptions to prevent memory holds.
- **Impact**: Ensures Q2.3 24h+ runtime without degradation.
- **Trade-offs**: Refactor for hosted service pattern.
- **Test**: Memory profiling in benchmarks.

## 5. General Optimizations

### G1: Logging and Debug Overhead
- **Issue**: Extensive Console.WriteLine and LogDebug in hot paths (e.g., HookCallback, event handlers).
- **Change**: Replace with conditional compilation (#if DEBUG) or ILogger at Trace level. Remove all Console.WriteLine for production.
- **Impact**: Idle CPU <0.5%; active <3%.

### G2: Compilation and Runtime
- **Issue**: Potential debug builds in CI.
- **Change**: Enforce Release mode with optimizations (/optimize+) in build.ps1. Use AOT if .NET 7+ for faster startup.
- **Impact**: Startup <200ms (P4.1); overall efficiency.

### G3: Benchmark Integration
- Add new benchmarks for hook callback and volume processing. Update CI to fail on regressions (CI2.1-CI2.4).

## Implementation Plan
1. Apply InputService changes; benchmark latency.
2. Tune AudioService; verify with WASAPI loopback.
3. Refactor Controller and Memory; profile with dotMemory/WPR.
4. Update tests/docs; merge to feature branch.
5. Run full CI/CD on Win10/11.

These changes would achieve all performance targets while maintaining functionality. Estimated effort: 8-12 hours development + testing.

Revision: 1.0 | Date: 2025-09-06
