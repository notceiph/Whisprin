#!/usr/bin/env python3
"""
Generate a simple pencil-like audio loop for testing Artisense.
Requires: pip install numpy scipy
"""

import numpy as np
import scipy.io.wavfile as wav
import os

def generate_pencil_sound(duration=0.5, sample_rate=44100):
    """Generate a simple pencil-on-paper like sound."""
    
    # Time array
    t = np.linspace(0, duration, int(sample_rate * duration), False)
    
    # Create a base noise (simulating paper texture)
    white_noise = np.random.normal(0, 0.1, len(t))
    
    # Add some filtered noise for pencil texture
    # Use pink noise characteristics (1/f noise)
    frequencies = np.fft.fftfreq(len(t), 1/sample_rate)
    fft_noise = np.fft.fft(white_noise)
    
    # Apply pink noise filter (1/f^0.5)
    pink_filter = np.where(frequencies != 0, 1/np.sqrt(np.abs(frequencies)), 1)
    pink_filter[0] = 1  # Avoid division by zero at DC
    
    fft_filtered = fft_noise * pink_filter
    pink_noise = np.real(np.fft.ifft(fft_filtered))
    
    # Normalize
    pink_noise = pink_noise / np.max(np.abs(pink_noise)) * 0.3
    
    # Add subtle harmonic content for more realistic sound
    harmonic = 0.1 * np.sin(2 * np.pi * 2000 * t) * np.exp(-t * 5)
    harmonic += 0.05 * np.sin(2 * np.pi * 3000 * t) * np.exp(-t * 8)
    
    # Combine components
    sound = pink_noise + harmonic
    
    # Apply envelope for smooth looping
    envelope = np.ones_like(t)
    fade_samples = int(0.05 * sample_rate)  # 50ms fade
    envelope[:fade_samples] = np.linspace(0, 1, fade_samples)
    envelope[-fade_samples:] = np.linspace(1, 0, fade_samples)
    
    sound *= envelope
    
    # Normalize to 16-bit range
    sound = np.clip(sound * 32767, -32768, 32767).astype(np.int16)
    
    return sound, sample_rate

def main():
    """Generate the test audio file."""
    print("Generating pencil sound...")
    
    # Generate audio
    audio_data, sample_rate = generate_pencil_sound()
    
    # Ensure output directory exists
    output_dir = "../src/Artisense.UI/Assets"
    os.makedirs(output_dir, exist_ok=True)
    
    # Write WAV file
    output_path = os.path.join(output_dir, "pencil_loop.wav")
    wav.write(output_path, sample_rate, audio_data)
    
    print(f"Generated: {output_path}")
    print(f"Duration: {len(audio_data) / sample_rate:.2f} seconds")
    print(f"Sample rate: {sample_rate} Hz")
    print(f"File size: {os.path.getsize(output_path)} bytes")

if __name__ == "__main__":
    main()
