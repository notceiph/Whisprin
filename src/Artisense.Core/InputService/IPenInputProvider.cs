// Copyright (c) Artisense. All rights reserved.

using System;

namespace Artisense.Core.InputService
{
    /// <summary>
    /// Interface for pen input providers that can capture global pen/stylus input.
    /// </summary>
    public interface IPenInputProvider : IDisposable
    {
        /// <summary>
        /// Occurs when the pen makes contact with the screen.
        /// </summary>
        event EventHandler<PenEventArgs>? PenDown;

        /// <summary>
        /// Occurs when the pen moves while in contact with the screen.
        /// </summary>
        event EventHandler<PenEventArgs>? PenMove;

        /// <summary>
        /// Occurs when the pen is lifted from the screen.
        /// </summary>
        event EventHandler? PenUp;

        /// <summary>
        /// Gets a value indicating whether the provider is currently active and listening for input.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Starts listening for pen input events.
        /// </summary>
        /// <returns>True if successfully started, false otherwise.</returns>
        bool Start();

        /// <summary>
        /// Stops listening for pen input events.
        /// </summary>
        void Stop();
    }
}
