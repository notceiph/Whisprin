# PowerShell script to generate a simple test audio file for Artisense
# Creates a basic white noise burst that simulates pencil-on-paper sound

param(
    [string]$OutputPath = "../src/Artisense.UI/Assets/pencil_loop.wav",
    [double]$Duration = 0.5,
    [int]$SampleRate = 44100
)

Write-Host "Generating test audio for Artisense..." -ForegroundColor Green

# Calculate audio parameters
$TotalSamples = [int]($Duration * $SampleRate)
$BytesPerSample = 2  # 16-bit
$Channels = 1        # Mono
$BlockAlign = $Channels * $BytesPerSample
$ByteRate = $SampleRate * $BlockAlign
$DataSize = $TotalSamples * $BlockAlign
$FileSize = 36 + $DataSize

Write-Host "Duration: $Duration seconds"
Write-Host "Sample Rate: $SampleRate Hz"
Write-Host "Total Samples: $TotalSamples"
Write-Host "File Size: $FileSize bytes"

# Ensure output directory exists
$OutputDir = Split-Path $OutputPath -Parent
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Create WAV file header
$Header = [byte[]](
    # RIFF header
    [Text.Encoding]::ASCII.GetBytes("RIFF") +
    [BitConverter]::GetBytes([uint32]($FileSize - 8)) +
    [Text.Encoding]::ASCII.GetBytes("WAVE") +
    
    # fmt chunk
    [Text.Encoding]::ASCII.GetBytes("fmt ") +
    [BitConverter]::GetBytes([uint32]16) +      # fmt chunk size
    [BitConverter]::GetBytes([uint16]1) +       # PCM format
    [BitConverter]::GetBytes([uint16]$Channels) +
    [BitConverter]::GetBytes([uint32]$SampleRate) +
    [BitConverter]::GetBytes([uint32]$ByteRate) +
    [BitConverter]::GetBytes([uint16]$BlockAlign) +
    [BitConverter]::GetBytes([uint16]16) +      # bits per sample
    
    # data chunk
    [Text.Encoding]::ASCII.GetBytes("data") +
    [BitConverter]::GetBytes([uint32]$DataSize)
)

Write-Host "Creating WAV file: $OutputPath"

# Create file stream
$FileStream = [System.IO.File]::Create($OutputPath)

try {
    # Write header
    $FileStream.Write($Header, 0, $Header.Length)
    
    # Generate audio data (simple noise pattern)
    $Random = New-Object System.Random
    $FadeSamples = [int]($SampleRate * 0.05)  # 50ms fade
    
    for ($i = 0; $i -lt $TotalSamples; $i++) {
        # Generate noise value (-1 to 1)
        $NoiseValue = ($Random.NextDouble() - 0.5) * 2.0
        
        # Apply simple envelope for looping
        $Envelope = 1.0
        if ($i -lt $FadeSamples) {
            $Envelope = $i / $FadeSamples
        } elseif ($i -ge ($TotalSamples - $FadeSamples)) {
            $Envelope = ($TotalSamples - $i) / $FadeSamples
        }
        
        # Scale to 16-bit range with envelope
        $Sample = [int16]($NoiseValue * $Envelope * 8000)  # Moderate volume
        
        # Write sample as little-endian 16-bit
        $SampleBytes = [BitConverter]::GetBytes($Sample)
        $FileStream.Write($SampleBytes, 0, 2)
    }
    
    Write-Host "✅ Test audio generated successfully!" -ForegroundColor Green
    Write-Host "File: $OutputPath"
    Write-Host "Size: $((Get-Item $OutputPath).Length) bytes"
    
} finally {
    $FileStream.Close()
}

Write-Host "`n🔧 Next steps:" -ForegroundColor Yellow
Write-Host "1. Rebuild the application:"
Write-Host "   dotnet build src/Artisense.UI/Artisense.UI.csproj --configuration Release"
Write-Host "2. Run the application:"
Write-Host "   dotnet run --project src/Artisense.UI/Artisense.UI.csproj"
Write-Host "3. Test with a pressure-sensitive stylus in any drawing app"
