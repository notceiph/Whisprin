// Copyright (c) Artisense. All rights reserved.

using System;

namespace Artisense.Core.InputService
{
    /// <summary>
    /// Event arguments for pen input events.
    /// </summary>
    public class PenEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PenEventArgs"/> class.
        /// </summary>
        /// <param name="pressure">The normalized pressure value (0.0 to 1.0).</param>
        public PenEventArgs(float pressure)
        {
            Pressure = Math.Clamp(pressure, 0.0f, 1.0f);
        }

        /// <summary>
        /// Gets the normalized pressure value from 0.0 (no pressure) to 1.0 (maximum pressure).
        /// </summary>
        public float Pressure { get; }
    }
}
