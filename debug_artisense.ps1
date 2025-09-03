# Debug script for Artisense troubleshooting
# Run this to check various aspects of the system

Write-Host "🔍 Artisense Debugging Tool" -ForegroundColor Green
Write-Host "================================"

# Check if we are running with a stylus-capable device
Write-Host "`n📝 Checking Input Devices:" -ForegroundColor Yellow
$tablets = Get-WmiObject -Class Win32_PointingDevice | Where-Object { $_.Description -like "*pen*" -or $_.Description -like "*stylus*" -or $_.Description -like "*tablet*" }
if ($tablets) {
    Write-Host "✅ Pen/Stylus devices found:"
    $tablets | ForEach-Object { Write-Host "   - $($_.Description)" }
} else {
    Write-Host "⚠️  No pen/stylus devices detected" -ForegroundColor Yellow
    Write-Host "   This may be why pen input is not working"
}

# Check audio devices
Write-Host "`n🔊 Checking Audio Devices:" -ForegroundColor Yellow
$audioDevices = Get-WmiObject -Class Win32_SoundDevice
if ($audioDevices) {
    Write-Host "✅ Audio devices found:"
    $audioDevices | ForEach-Object { Write-Host "   - $($_.Name)" }
} else {
    Write-Host "❌ No audio devices found" -ForegroundColor Red
}

# Check if Artisense is running
Write-Host "`n🏃 Checking Running Processes:" -ForegroundColor Yellow
$artisenseProcess = Get-Process -Name "Artisense.UI" -ErrorAction SilentlyContinue
if ($artisenseProcess) {
    Write-Host "✅ Artisense.UI is running (PID: $($artisenseProcess.Id))"
} else {
    Write-Host "❌ Artisense.UI is not running" -ForegroundColor Red
}

# Check Windows Ink settings
Write-Host "`n🖊️ Windows Ink Status:" -ForegroundColor Yellow
try {
    $inkSettings = Get-ItemProperty "HKCU:\Software\Microsoft\TabletTip\1.7" -ErrorAction SilentlyContinue
    if ($inkSettings) {
        Write-Host "✅ Windows Ink settings found"
    } else {
        Write-Host "⚠️  Windows Ink settings not found"
    }
} catch {
    Write-Host "⚠️  Could not check Windows Ink settings"
}

# Test if the audio file exists
Write-Host "`n🎵 Checking Audio File:" -ForegroundColor Yellow
$audioFile = "src\Artisense.UI\Assets\pencil_loop.wav"
if (Test-Path $audioFile) {
    $fileInfo = Get-Item $audioFile
    Write-Host "✅ Audio file exists: $($fileInfo.Length) bytes"
} else {
    Write-Host "❌ Audio file not found at: $audioFile" -ForegroundColor Red
}

# Check if the built executable exists
Write-Host "`n📦 Checking Built Application:" -ForegroundColor Yellow
$builtApp = "artifacts\Artisense.UI.exe"
if (Test-Path $builtApp) {
    $appInfo = Get-Item $builtApp
    Write-Host "✅ Built application exists: $($appInfo.Length) bytes"
    Write-Host "   Last modified: $($appInfo.LastWriteTime)"
} else {
    Write-Host "❌ Built application not found at: $builtApp" -ForegroundColor Red
}

# Check debug build
$debugApp = "artifacts-debug\Artisense.UI.exe"
if (Test-Path $debugApp) {
    $debugInfo = Get-Item $debugApp
    Write-Host "✅ Debug build exists: $($debugInfo.Length) bytes"
    Write-Host "   Last modified: $($debugInfo.LastWriteTime)"
} else {
    Write-Host "⚠️  Debug build not found at: $debugApp" -ForegroundColor Yellow
}

Write-Host "`n🔧 Troubleshooting Suggestions:" -ForegroundColor Cyan
Write-Host "1. If no pen devices detected:"
Write-Host "   - Ensure your tablet/stylus is properly connected"
Write-Host "   - Check Device Manager for pen/tablet devices"
Write-Host "   - Install latest drivers for your tablet"

Write-Host "`n2. If no audio devices found:"
Write-Host "   - Check Windows Sound settings"
Write-Host "   - Ensure speakers/headphones are connected"
Write-Host "   - Try running as Administrator"

Write-Host "`n3. To test pen input manually:"
Write-Host "   - Open Windows Ink Workspace"
Write-Host "   - Try drawing in Sketchpad"
Write-Host "   - If that does not work, the issue is with pen setup"

Write-Host "`n4. To test with console output:"
Write-Host "   - Close any running Artisense instances"
Write-Host "   - Run: cd artifacts-debug && .\Artisense.UI.exe"
Write-Host "   - Look for log messages when drawing"

Write-Host "`n📝 Run this script periodically to check system status" -ForegroundColor Green