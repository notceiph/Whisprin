# Artisense Build Script
# Builds, tests, and packages the Artisense MVP

param(
    [string]$Configuration = "Release",
    [switch]$SkipTests = $false,
    [switch]$PublishSingleFile = $true
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Building Artisense MVP" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow

# Restore dependencies
Write-Host "`n📦 Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore Artisense.sln

# Build solution
Write-Host "`n🔨 Building solution..." -ForegroundColor Cyan
dotnet build Artisense.sln --configuration $Configuration --no-restore --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Run tests
if (-not $SkipTests) {
    Write-Host "`n🧪 Running tests..." -ForegroundColor Cyan
    dotnet test tests/Artisense.Tests/Artisense.Tests.csproj --configuration $Configuration --no-build --verbosity minimal --collect:"XPlat Code Coverage"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

# Run static analysis
Write-Host "`n🔍 Running static analysis..." -ForegroundColor Cyan
$warnings = dotnet build Artisense.sln --configuration $Configuration --verbosity diagnostic | Select-String "warning SA"
if ($warnings) {
    Write-Warning "StyleCop warnings found:"
    $warnings | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}

# Publish single-file executable
if ($PublishSingleFile) {
    Write-Host "`n📦 Publishing single-file executable..." -ForegroundColor Cyan
    
    $publishArgs = @(
        "publish"
        "src/Artisense.UI/Artisense.UI.csproj"
        "--configuration", $Configuration
        "--runtime", "win-x64"
        "--self-contained", "false"
        "--output", "./artifacts/"
        "--verbosity", "minimal"
    )
    
    if ($PublishSingleFile) {
        $publishArgs += "--property:PublishSingleFile=true"
        $publishArgs += "--property:IncludeNativeLibrariesForSelfExtract=true"
    }
    
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Publish failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    
    # Check executable size
    $exePath = "./artifacts/Artisense.UI.exe"
    if (Test-Path $exePath) {
        $sizeBytes = (Get-Item $exePath).Length
        $sizeMB = [math]::Round($sizeBytes / 1MB, 2)
        Write-Host "📊 Executable size: $sizeMB MB" -ForegroundColor Green
        
        if ($sizeMB -gt 6) {
            Write-Warning "Executable size ($sizeMB MB) exceeds 6MB target"
        }
    }
}

# Run benchmarks
Write-Host "`n⚡ Running performance benchmarks..." -ForegroundColor Cyan
try {
    & dotnet run --project tests/Artisense.Tests/Artisense.Tests.csproj --configuration $Configuration --framework net6.0 -- --job short --filter "*LatencyBenchmark*"
} catch {
    Write-Warning "Benchmarks failed to run (this is expected in some environments)"
}

Write-Host "`n✅ Build completed successfully!" -ForegroundColor Green
Write-Host "Artifacts are available in: ./artifacts/" -ForegroundColor Yellow

if (Test-Path "./artifacts/Artisense.UI.exe") {
    Write-Host "🎯 Ready to deploy: Artisense.UI.exe" -ForegroundColor Green
}
