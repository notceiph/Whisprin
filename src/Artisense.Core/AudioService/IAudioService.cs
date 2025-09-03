// Copyright (c) Artisense. All rights reserved.

using System;

namespace Artisense.Core.AudioService
{
    /// <summary>
    /// Interface for audio playback services that provide pressure-sensitive sound output.
    /// </summary>
    public interface IAudioService : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the audio service is currently playing.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Gets or sets the volume offset in decibels (-12 to 0).
        /// </summary>
        float VolumeOffsetDb { get; set; }

        /// <summary>
        /// Starts audio playback with the specified pressure.
        /// </summary>
        /// <param name="pressure">The initial pressure value (0.0 to 1.0).</param>
        void Start(float pressure);

        /// <summary>
        /// Updates the pressure value during playback.
        /// </summary>
        /// <param name="pressure">The new pressure value (0.0 to 1.0).</param>
        void SetPressure(float pressure);

        /// <summary>
        /// Stops audio playback.
        /// </summary>
        void Stop();
    }
}
