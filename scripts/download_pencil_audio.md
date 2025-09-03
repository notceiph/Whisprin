# Adding Real Pencil Audio to Artisense

## Option A: Quick Generated Audio (Recommended for testing)

1. **Install Python dependencies:**
   ```bash
   pip install numpy scipy
   ```

2. **Run the audio generator:**
   ```bash
   cd scripts
   python generate_test_audio.py
   ```

3. **Rebuild the application:**
   ```bash
   dotnet build src/Artisense.UI/Artisense.UI.csproj --configuration Release
   ```

## Option B: Download Real Pencil Sounds

### Free Sound Resources:

1. **Freesound.org** (requires free account):
   - Search for "pencil writing", "pencil on paper", "drawing"
   - Download a short (0.5-1 second) loop-friendly sample
   - Convert to WAV format if needed

2. **Recommended searches:**
   - "pencil scratch paper"
   - "drawing pencil texture"
   - "writing sound"

### Audio Requirements:

- **Format:** 16-bit WAV, 44.1kHz
- **Duration:** 0.3-1.0 seconds (for seamless looping)
- **Volume:** Moderate level (will be controlled by pressure)
- **Loop-friendly:** Should sound natural when repeated

## Option C: Record Your Own

1. **Record with phone/microphone:**
   - Use a real pencil on paper
   - Record 2-3 seconds of continuous drawing
   - Save as high-quality audio

2. **Process the audio:**
   - Trim to 0.5-1 second
   - Normalize volume
   - Ensure smooth loop points
   - Convert to 44.1kHz WAV

3. **Tools for audio editing:**
   - **Audacity** (free): https://www.audacityteam.org/
   - **Windows Voice Recorder** (basic)
   - **Online converters** for format conversion

## Installation Steps:

1. **Replace the placeholder file:**
   ```
   src/Artisense.UI/Assets/pencil_loop.wav
   ```

2. **Ensure file is set as Embedded Resource:**
   - File should be included in `Artisense.UI.csproj` under `<EmbeddedResource>`

3. **Rebuild and test:**
   ```bash
   dotnet build src/Artisense.UI/Artisense.UI.csproj --configuration Release
   dotnet run --project src/Artisense.UI/Artisense.UI.csproj
   ```

## Testing the Audio:

1. **Run the application:**
   ```bash
   cd artifacts
   ./Artisense.UI.exe
   ```

2. **Check system tray:**
   - Look for Artisense icon in system tray
   - Right-click to access controls

3. **Test with stylus/pen:**
   - Open any drawing application (Paint, etc.)
   - Use pressure-sensitive stylus
   - Should hear pencil sound varying with pressure

## Troubleshooting:

- **No audio device errors:** Try regular WASAPI mode instead of exclusive
- **Audio format errors:** Ensure WAV file is 16-bit, 44.1kHz
- **File not found errors:** Check embedded resource configuration
- **Permission errors:** Run as administrator if needed

## Audio File Specifications:

```
Format: WAV (PCM)
Sample Rate: 44100 Hz
Bit Depth: 16-bit
Channels: Mono or Stereo
Duration: 0.3-1.0 seconds
Size: <100KB recommended
```
