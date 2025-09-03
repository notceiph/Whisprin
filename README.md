# Artisense MVP

**Pressure-sensitive pencil sounds for digital drawing on Windows**

Artisense adds realistic pencil-on-paper sounds that respond to your stylus pressure, making digital drawing feel more natural and engaging.

## Features

- 🎨 **Global pen detection** - Works in any drawing application
- 🔊 **Pressure-sensitive audio** - Volume responds naturally to pen pressure  
- ⚡ **Ultra-low latency** - <15ms response time for real-time feel
- 🪶 **Lightweight** - <30MB memory, <5% CPU during use
- 🎯 **Single-file deployment** - No installation required
- 🔧 **Simple controls** - Enable/disable and volume adjustment via system tray

## Compatibility

- **Windows 10** (21H2+) and **Windows 11** (23H2+)
- **Surface Pen** and **Wacom** stylus devices
- Any **WASAPI-compatible** audio output

## Quick Start

1. Download `Artisense.UI.exe` from [Releases](../../releases)
2. Run the executable - it will appear in your system tray
3. Start drawing in any application with a pressure-sensitive stylus
4. Right-click the tray icon to adjust settings or disable

## System Tray Controls

- **✔ Sound Enabled** - Toggle audio on/off
- **Volume Offset** - Adjust overall volume (-12dB to 0dB)  
- **Exit** - Close the application

## Technical Details

- **Audio Engine**: WASAPI exclusive mode for minimal latency
- **Input Detection**: Raw Input API with Wintab fallback
- **Volume Curve**: Perceptual loudness (pressure^0.6) with smoothing
- **Architecture**: .NET 6 with hosted services for reliability

## Performance Targets

| Metric | Target | Measured |
|--------|--------|----------|
| End-to-end latency | ≤15ms p50 | ✅ |
| CPU usage (idle) | <1% | ✅ |
| CPU usage (drawing) | <5% | ✅ |
| Memory footprint | <30MB | ✅ |
| Startup time | <300ms | ✅ |
| Executable size | <6MB | ✅ |

## Development

### Building from Source

```bash
# Clone the repository
git clone https://github.com/username/Artisense.git
cd Artisense

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run tests
dotnet test

# Publish single-file executable
dotnet publish src/Artisense.UI/Artisense.UI.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

### Project Structure

```
src/
├── Artisense.Core/     # Core services (input, audio, controller)
└── Artisense.UI/       # WPF tray application
tests/
└── Artisense.Tests/    # Unit tests and benchmarks
docs/
├── requirements.md     # Formal requirements specification
└── design-notes.md     # Architecture and design decisions
```

### Contributing

1. Fork the repository
2. Create a feature branch
3. Ensure all tests pass and coverage >90%
4. Submit a pull request

## License

Copyright (c) 2025 Artisense. All rights reserved.

## Acknowledgments

- **NAudio** - Professional audio library for .NET
- **Hardcodet.NotifyIcon.Wpf** - WPF system tray support
- **BenchmarkDotNet** - Performance measurement framework
