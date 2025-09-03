# Enhanced *Artisense* MVP Specification (2025-09)

The outline below folds in all performance, reliability, and deployment optimizations discussed earlier while keeping the scope laser-focused on **one pressure-sensitive pencil-on-paper loop that works everywhere in Windows**.

***

## 1. Core Value Proposition (unchanged)

Deliver a **global, latency-free, pressure-controlled pencil-stroke sound** that runs unobtrusively in the background on Windows.

***

## 2. Technology Stack & High-Level Architecture

| Concern | Decision | Rationale |
|---|---|---|
| Language / Runtime | C# 10 on .NET 6 LTS | Mature desktop stack, fast AOT-ready; broad driver support |
| GUI Framework | WPF (tray-only) | Small footprint; XAML optional | 
| Global Pen Input | **Raw Input (`RegisterRawInputDevices`)** with optional Wintab fallback | Avoids admin privileges and thread-wide hooks; minimal CPU |
| Audio Engine | **NAudio + WASAPI Exclusive** (ASIO optional) | <10 ms latency, click-free looping |
| DI / Lifetime | Built-in `Microsoft.Extensions.Hosting` generic host | Clean shutdown; background services |
| Packaging | Costura.Fody single-file exe; assets embedded as resources | True drag-and-run distribution |

### Layer Diagram
```
┌─────────┐
│  Tray   │─┐
└─────────┘ │
             ▼ UI events
┌───────────────────────┐
│ CoreController (Host) │
└───────────────────────┘
      ▲ pen data   │ audio cmds
┌──────────────┐ ┌──────────────┐
│ InputService │ │ AudioService │
└──────────────┘ └──────────────┘
```

***

## 3. Feature Breakdown & Milestones

### Module 1 – **InputService** (global pen capture)
1. Register Raw Input for `RIM_TYPEHID` with usage pages matching stylus devices.
2. Parse `RAWHID` for:
   - `isInContact`
   - `pressure (0-1,024)` → normalized float.
3. **Events:** `PenDown(p)`, `PenMove(p)`, `PenUp()`.
4. Fallback: if no HID stylus detected, load **Wintab32** dynamically and expose the same events.

*Unit Test:* Synthetic HID packets → expect correct normalization.

***

### Module 2 – **AudioService** (real-time loop)
1. Load embedded `pencil_loop.wav` into memory (`MemoryStream`).
2. Instantiate NAudio `WasapiOut` (exclusive) *lazily on first PenDown*; dispose after 5 s idle.
3. Wrap buffer in custom `LoopStream` aligned to 128-sample boundary.
4. Apply:
   - **Perceptual loudness curve:** `volume = pressure^0.6`.
   - **10 ms low-pass envelope** to smooth jitter.
5. Public API: `Start(p)`, `SetPressure(p)`, `Stop()`.

*Bench Test:* Measure contact-to-sound latency via loopback: target <15 ms.

***

### Module 3 – **CoreController**
1. Subscribes to `InputService` events.
2. On `PenDown` → `AudioService.Start(p)`.
3. On `PenMove` → `AudioService.SetPressure(p)`.
4. On `PenUp` → `AudioService.Stop()`.

***

### Module 4 – **Tray UI**
1. No main window; startup via `App.xaml` → Host builder.
2. Tray menu (Hardcodet.NotifyIcon.Wpf):
   - “✔ Sound Enabled” (checkable)
   - “Volume Offset” slider (-12 dB ↔ 0 dB) & curve preset dropdown
   - “Exit”
3. Toggle simply gates CoreController; exit triggers graceful Host stop.

***

## 4. Definition of Done (enhanced)

1. Runs as a **single EXE**, <6 MB, no external files.
2. Starts to tray in <300 ms; no admin rights required.
3. Global pressure-driven sound works in any app that emits stylus HID packets (Photoshop, Krita, Paint).
4. End-to-end latency ≤15 ms; idle CPU <1%, active CPU <5% on mid-range laptop.
5. User can disable sound, tweak offset, or exit—all without leaks (WPA shows zero unreleased handles).
6. If InputService crashes, Host restarts it automatically (background service restart policy).
7. Verified on Windows 10 21H2 and Windows 11 23H2 with Surface Pen & Wacom Intuos.

***

## 5. Post-MVP Extension Points

- Additional material loops (canvas, marker) selectable in tray.
- Velocity → pitch modulation.
- Haptic out via Surface Dial or Wacom motors.

By adopting Raw Input, WASAPI exclusive playback, and a hosted service architecture, this MVP remains **lightweight, low-latency, and safe**, ready for rapid future expansion without re-architecture.