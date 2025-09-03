# Changelog

All notable changes to the Artisense project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial Artisense MVP implementation
- Global pen input detection using Raw Input API
- Fallback Wintab support for legacy devices
- Real-time audio playback with WASAPI exclusive mode
- Pressure-sensitive volume control with perceptual curve
- Low-latency audio loop with seamless playback
- System tray interface with enable/disable toggle
- Volume offset control (-12dB to 0dB)
- Single-file executable deployment
- Comprehensive unit test suite with >90% coverage
- Performance benchmarks for latency and CPU usage
- CI/CD pipeline with automated testing and packaging

### Technical Details
- **Latency**: Target ≤15ms p50, ≤20ms p95 end-to-end
- **CPU Usage**: <1% idle, <5% active drawing
- **Memory**: <30MB peak usage with zero handle leaks
- **Compatibility**: Windows 10 21H2 and Windows 11 23H2
- **Dependencies**: .NET 6 LTS, WPF, NAudio, Raw Input API

### Architecture
- **Input Layer**: Raw Input HID parsing with Wintab fallback
- **Audio Layer**: WASAPI exclusive mode with custom loop stream
- **Controller**: Event-driven coordination between input and audio
- **UI Layer**: WPF system tray with Hardcodet.NotifyIcon
- **Packaging**: Costura.Fody single-file deployment

### Quality Assurance
- StyleCop and SonarLint static analysis
- BenchmarkDotNet performance testing
- xUnit unit tests with FluentAssertions
- Automated CI/CD pipeline with performance gates
- Memory leak detection with dotMemory integration

## [1.0.0] - 2025-01-27

### Added
- Initial release of Artisense MVP
- Core functionality for pressure-sensitive drawing sounds
- Single-file executable deployment ready for distribution
