# Step-By-Step Development Plan for a “Fail-Proof” *Artisense* MVP  

The roadmap below blends classic **V-Model** rigor (each development step paired with a validation step) and modern DevOps automation so defects are caught the moment they appear. Follow the phases in order; do not overlap milestones until all exit criteria are satisfied.

***

## Phase 0 – Project Foundations  

| Activity | Deliverable | Fail-Proof Controls |
|---|---|---|
| Define acceptance criteria (latency, CPU, single-exe, Windows versions). | Formal requirements doc in repo (`docs/requirements.md`). | All CI gates use these numbers; merges blocked if benchmarks regress. |
| Set up Git repo with trunk-based workflow (`main` + short-lived feature branches). | GitHub / Azure DevOps project. | Protected `main`; mandatory pull-request reviews + status checks. |
| Configure CI/CD pipeline (GitHub Actions) with matrix for Win10 21H2 & Win11 23H2. | `./.github/workflows/build.yml`. | Each PR builds, runs unit tests, static analyzers (Sonar, StyleCop), and publishes artifacts. |

***

## Phase 1 – InputService  

1. **Spike-Prototype (1 day)**  
   -  Console app registering Raw Input → dump packets to log.  
   -  Exit Criterion: pressure data visible.

2. **Production Implementation (2 days)**  
   -  `InputService` class library + HID parsing helpers.  
   -  Interface: `IPenInputProvider`.  

3. **Unit Tests (1 day)**  
   -  Feed recorded HID packets (binary fixtures) into parser; assert normalized values.  

4. **Integration Test (½ day)**  
   -  WPF test harness displays live pressure graph.  

*Fail-Proof Guards*  
-  Memory leak check via `dotMemoryUnit`.  
-  Static analysis: ensure no P/Invoke marshaling errors.

***

## Phase 2 – AudioService  

1. **Loop Asset Creation (½ day)**  
   -  Record, edit, export `pencil_loop.wav`; commit as embedded resource.  

2. **Core Audio Engine (2 days)**  
   -  Load stream, wrap in 128-sample-aligned `LoopStream`.  
   -  WASAPI exclusive path with lazy open/close.  

3. **DSP Layer (1 day)**  
   -  Loudness curve + 10 ms smoothing filter.  

4. **Unit Tests (1 day)**  
   -  Verify curve maths; envelope reaches steady-state within 50 ms.  

5. **Latency Benchmark (½ day)**  
   -  Scripted Tone burst; measure via loopback; store result in CI artifact; fail if >15 ms.

***

## Phase 3 – CoreController  

1. **Glue Code (1 day)**  
   -  Subscribe to `InputService`; command `AudioService`.  

2. **Behavioral Tests (1 day)**  
   -  Simulated pen script (down→moves→up) should trigger correct start/stop & volume trace.  

3. **Resilience Test (½ day)**  
   -  Kill `AudioService` thread; ensure Host restarts it; no crash.

***

## Phase 4 – Tray UI  

1. **Tray Skeleton (½ day)**  
   -  Hardcodet.NotifyIcon; menu items stubbed.  

2. **Bindings to Controller (1 day)**  
   -  Enabled checkbox gates audio; slider adjusts volume offset in real time.  

3. **UX Verification (½ day)**  
   -  Runs headless; startup <300 ms measured by `perfview /timing`.  

***

## Phase 5 – Packaging & Deployment  

1. **Costura.Fody Merge (½ day)**  
   -  Single-file exe created in Release build.  

2. **Installer Optional (1 day)**  
   -  Inno Setup script (adds Start menu link, no admin).  

3. **VirusTotal & SmartScreen Check (½ day)**  
   -  Submit binary; resolve flags.

***

## Phase 6 – System Validation  

| Test Suite | Tool | Pass Threshold |
|---|---|---|
| CPU profiling | Windows Performance Recorder (WPR/WPA) | Idle <1%, draw <5% |
| Memory | PerfMon, dotMemory | Peak <30 MB, zero undisposed handles |
| Latency | WASAPI loopback scope | ≤15 ms mean, ≤20 ms p95 |
| Multi-App Stress | AutoHotkey script draws in 3 apps simultaneously for 10 min | No audio glitches, no missed packets |

All suites run nightly; failures block next-day merges.

***

## Phase 7 – UAT & Sign-Off  

1. **Closed Beta (3 test artists)** – 1 week  
   -  Collect logs via integrated Serilog + Seq.  
   -  Surface bug reports in GitHub Issues.

2. **Bug-Fix Sprint (≤1 week)**  
3. **Release Candidate Tag → GitHub Release**  

***

## Phase 8 – Post-Release Maintenance  

-  Set up crash telemetry (Sentry).  
-  Monthly dependency audit (NAudio, .NET patches).  
-  Roadmap grooming for velocity-pitch, extra materials, haptics.

***

### Built-In Fail-Safes Summary  

- **CI Gates:** Build, unit, integration, static analysis, latency & CPU benchmarks.  
- **Runtime Guards:** Separate hosted services, auto-restart, lazy audio device, fallback Wintab.  
- **Deploy Confidence:** Single-exe, VirusTotal clean, SmartScreen reputation build, optional installer.  

By rigorously pairing each build activity with an automated validation step and enforcing non-negotiable performance thresholds in CI, this plan virtually eliminates regressions and leads to a robust, user-ready MVP.