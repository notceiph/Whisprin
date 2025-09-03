// Copyright (c) Artisense. All rights reserved.

using System;
using NAudio.Wave;

namespace Artisense.Core.AudioService
{
    /// <summary>
    /// Audio processor that applies pressure-based volume control with smoothing.
    /// </summary>
    public class PressureVolumeProcessor : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly float[] smoothingBuffer;
        private float targetVolume;
        private float currentVolume;
        private float volumeOffsetDb;
        #pragma warning disable CS0414, S2933 // Field assigned but never used - reserved for future smoothing implementation
        private int smoothingIndex = 0;
        #pragma warning restore CS0414, S2933

        /// <summary>
        /// Initializes a new instance of the <see cref="PressureVolumeProcessor"/> class.
        /// </summary>
        /// <param name="source">The source sample provider.</param>
        public PressureVolumeProcessor(ISampleProvider source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            
            // 10ms smoothing buffer at 44.1kHz
            var smoothingSamples = (int)(source.WaveFormat.SampleRate * 0.01f);
            smoothingBuffer = new float[smoothingSamples];
            
            targetVolume = 0.0f;
            currentVolume = 0.0f;
            volumeOffsetDb = 0.0f;
        }

        /// <inheritdoc/>
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <summary>
        /// Gets or sets the volume offset in decibels (-12 to 0).
        /// </summary>
        public float VolumeOffsetDb
        {
            get => volumeOffsetDb;
            set => volumeOffsetDb = Math.Clamp(value, -12.0f, 0.0f);
        }

        /// <summary>
        /// Sets the target pressure value (0.0 to 1.0).
        /// </summary>
        /// <param name="pressure">The pressure value.</param>
        public void SetPressure(float pressure)
        {
            // Apply perceptual loudness curve: volume = pressure^0.6
            var normalizedPressure = Math.Clamp(pressure, 0.0f, 1.0f);
            var perceptualVolume = MathF.Pow(normalizedPressure, 0.6f);
            
            // Apply volume offset
            var offsetMultiplier = MathF.Pow(10.0f, volumeOffsetDb / 20.0f);
            targetVolume = perceptualVolume * offsetMultiplier;
        }

        /// <inheritdoc/>
        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = source.Read(buffer, offset, count);
            
            if (samplesRead == 0)
            {
                return 0;
            }

            // Apply smoothed volume control
            for (int i = 0; i < samplesRead; i++)
            {
                // Low-pass filter for smooth volume transitions
                var alpha = 1.0f / smoothingBuffer.Length;
                currentVolume += alpha * (targetVolume - currentVolume);
                
                // Apply volume to sample
                buffer[offset + i] *= currentVolume;
            }

            return samplesRead;
        }
    }
}
