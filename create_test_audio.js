// Simple Node.js script to create a minimal WAV file for testing
// Run with: node create_test_audio.js

const fs = require('fs');
const path = require('path');

function createSimpleWAV() {
    const sampleRate = 44100;
    const duration = 0.5; // seconds
    const totalSamples = Math.floor(sampleRate * duration);
    const dataSize = totalSamples * 2; // 16-bit samples
    const fileSize = 36 + dataSize;

    // WAV header
    const header = Buffer.alloc(44);
    let pos = 0;

    // RIFF header
    header.write('RIFF', pos); pos += 4;
    header.writeUInt32LE(fileSize - 8, pos); pos += 4;
    header.write('WAVE', pos); pos += 4;

    // fmt chunk
    header.write('fmt ', pos); pos += 4;
    header.writeUInt32LE(16, pos); pos += 4; // fmt chunk size
    header.writeUInt16LE(1, pos); pos += 2;  // PCM format
    header.writeUInt16LE(1, pos); pos += 2;  // mono
    header.writeUInt32LE(sampleRate, pos); pos += 4;
    header.writeUInt32LE(sampleRate * 2, pos); pos += 4; // byte rate
    header.writeUInt16LE(2, pos); pos += 2;  // block align
    header.writeUInt16LE(16, pos); pos += 2; // bits per sample

    // data chunk
    header.write('data', pos); pos += 4;
    header.writeUInt32LE(dataSize, pos);

    // Create audio data (simple noise)
    const audioData = Buffer.alloc(dataSize);
    const fadeSamples = Math.floor(sampleRate * 0.05); // 50ms fade

    for (let i = 0; i < totalSamples; i++) {
        // Generate noise (-1 to 1)
        let sample = (Math.random() - 0.5) * 2.0;
        
        // Apply envelope for smooth looping
        let envelope = 1.0;
        if (i < fadeSamples) {
            envelope = i / fadeSamples;
        } else if (i >= totalSamples - fadeSamples) {
            envelope = (totalSamples - i) / fadeSamples;
        }
        
        // Convert to 16-bit integer
        const value = Math.floor(sample * envelope * 8000);
        audioData.writeInt16LE(value, i * 2);
    }

    // Combine header and data
    const wavFile = Buffer.concat([header, audioData]);
    
    // Write to file
    const outputPath = path.join('src', 'Artisense.UI', 'Assets', 'pencil_loop.wav');
    
    // Ensure directory exists
    const dir = path.dirname(outputPath);
    if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
    }
    
    fs.writeFileSync(outputPath, wavFile);
    
    console.log('✅ Test audio created:', outputPath);
    console.log('📊 Duration:', duration, 'seconds');
    console.log('📊 Sample rate:', sampleRate, 'Hz');
    console.log('📊 File size:', wavFile.length, 'bytes');
    console.log('\n🔧 Next steps:');
    console.log('1. Rebuild: dotnet build src/Artisense.UI/Artisense.UI.csproj --configuration Release');
    console.log('2. Run: dotnet run --project src/Artisense.UI/Artisense.UI.csproj');
}

createSimpleWAV();
