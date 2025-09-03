// Copyright (c) Artisense. All rights reserved.

using System;

namespace Artisense.Core.CoreController
{
    /// <summary>
    /// Interface for the main Artisense controller that coordinates input and audio services.
    /// </summary>
    public interface IArtisenseController : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the controller is currently enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets or sets the volume offset in decibels (-12 to 0).
        /// </summary>
        float VolumeOffsetDb { get; set; }

        /// <summary>
        /// Enables the controller to start processing pen input and generating audio.
        /// </summary>
        void Enable();

        /// <summary>
        /// Disables the controller to stop processing pen input and generating audio.
        /// </summary>
        void Disable();
    }
}
