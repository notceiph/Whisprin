# 🎧 Testing Artisense MVP - Audio Setup Guide

## Quick Start (2 minutes)

Since you need to test the application immediately, here are the fastest options:

### Option 1: Simple Audio File (Recommended)

1. **Download any short WAV file** (0.5-1 second):
   - Go to https://freesound.org/search/?q=pencil+paper
   - Or use any short sound effect WAV file you have
   - Or record 1 second of noise with Windows Voice Recorder

2. **Place the file:**
   ```
   src/Artisense.UI/Assets/pencil_loop.wav
   ```

3. **Rebuild and test:**
   ```bash
   cd /c/Users/Ceiph/Documents/GitHub/Whisprin
   /c/Program\ Files/dotnet/dotnet build src/Artisense.UI/Artisense.UI.csproj --configuration Release
   /c/Program\ Files/dotnet/dotnet run --project src/Artisense.UI/Artisense.UI.csproj
   ```

### Option 2: Test Without Audio First

You can test the application logic without audio by temporarily modifying the code:

1. **Edit the audio service to use a placeholder:**
   - The application will start and show the tray icon
   - Pen input will be detected and logged
   - Audio playback will be simulated (no crashes)

2. **Run the application:**
   ```bash
   /c/Program\ Files/dotnet/dotnet run --project src/Artisense.UI/Artisense.UI.csproj
   ```

3. **Check system tray:**
   - Look for the Artisense icon
   - Right-click for menu options
   - Verify the UI works

## Testing Steps

### 1. Application Startup
- ✅ Application starts without errors
- ✅ System tray icon appears
- ✅ No console errors or crashes

### 2. Tray Interface
- ✅ Right-click tray icon shows menu
- ✅ "Sound Enabled" toggle works
- ✅ Volume slider responds
- ✅ Exit option works

### 3. Pen Input (if you have a stylus)
- ✅ Open any drawing app (MS Paint, etc.)
- ✅ Use pressure-sensitive stylus
- ✅ Check console logs for pen events
- ✅ Audio should play when drawing (if audio file exists)

### 4. Performance Testing
- ✅ CPU usage stays low when idle
- ✅ Memory usage remains stable
- ✅ No memory leaks during extended use

## Current Status

The **core application builds and runs successfully**. The only missing piece is the audio asset, which can be:

1. **Any WAV file** renamed to `pencil_loop.wav`
2. **Generated** using the provided scripts
3. **Downloaded** from free sound libraries
4. **Recorded** using your phone/microphone

## Audio File Specifications

```
File: src/Artisense.UI/Assets/pencil_loop.wav
Format: WAV (PCM)
Sample Rate: 44.1kHz (preferred) or 48kHz
Bit Depth: 16-bit
Channels: Mono or Stereo
Duration: 0.3-1.0 seconds
Size: <100KB recommended
```

## Troubleshooting

### "Audio file not found" error:
1. Ensure file exists at exact path: `src/Artisense.UI/Assets/pencil_loop.wav`
2. Rebuild application after adding file
3. Check file permissions

### "No audio device" error:
- Try running with different audio settings
- Check Windows audio permissions
- Test with headphones/speakers connected

### "Access denied" errors:
- Run as Administrator if needed
- Check antivirus software blocking

### No tray icon:
- Check Windows notification area settings
- Look in hidden icons area
- Check console for startup errors

## Next Steps After Testing

1. **Add real pencil audio** for authentic experience
2. **Test with actual stylus device** (Surface Pen, Wacom, etc.)
3. **Verify pressure sensitivity** works correctly
4. **Test in multiple drawing applications**
5. **Performance profiling** if needed

## Success Criteria

✅ Application starts without crashes  
✅ Tray interface is functional  
✅ Pen input detection works (with or without audio)  
✅ Memory and CPU usage are reasonable  
✅ User can enable/disable functionality  

The **Artisense MVP is ready for testing** - just add any audio file to complete the experience!
