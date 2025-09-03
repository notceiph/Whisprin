// Copyright (c) Artisense. All rights reserved.

using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Artisense.Core.InputService
{
    /// <summary>
    /// Fallback pen input provider using Wintab API for legacy device compatibility.
    /// </summary>
    public class WintabPenProvider : IPenInputProvider
    {
        private readonly ILogger<WintabPenProvider> logger;
        private bool isActive;
        private bool isDisposed;
        #pragma warning disable CS0414, S2933 // Field assigned but never used - placeholder for Wintab implementation
        private bool isInContact = false;
        private float lastPressure = 0.0f;
        #pragma warning restore CS0414, S2933

        /// <summary>
        /// Initializes a new instance of the <see cref="WintabPenProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public WintabPenProvider(ILogger<WintabPenProvider> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
#pragma warning disable CS0067 // Event is never used - placeholder for Wintab implementation
        public event EventHandler<PenEventArgs>? PenDown;

        /// <inheritdoc/>
        public event EventHandler<PenEventArgs>? PenMove;

        /// <inheritdoc/>
        public event EventHandler? PenUp;
#pragma warning restore CS0067

        /// <inheritdoc/>
        public bool IsActive => isActive;

        /// <inheritdoc/>
        public bool Start()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(WintabPenProvider));
            }

            if (isActive)
            {
                return true;
            }

            try
            {
                // Check if Wintab is available
                if (!IsWintabAvailable())
                {
                    logger.LogWarning("Wintab is not available on this system");
                    return false;
                }

                // Initialize Wintab context
                // Note: This is a simplified implementation
                // Real implementation would properly initialize Wintab context
                
                isActive = true;
                logger.LogInformation("Wintab pen provider started successfully");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start Wintab pen provider");
                return false;
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!isActive)
            {
                return;
            }

            try
            {
                // Clean up Wintab context
                isActive = false;
                logger.LogInformation("Wintab pen provider stopped");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping Wintab pen provider");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the provider and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                Stop();
            }

            isDisposed = true;
        }

        private bool IsWintabAvailable()
        {
            try
            {
                // Try to load wintab32.dll dynamically
                var hModule = LoadLibrary("wintab32.dll");
                if (hModule == IntPtr.Zero)
                {
                    return false;
                }

                // Check if WTInfo function is available
                var wtInfoProc = GetProcAddress(hModule, "WTInfoW");
                FreeLibrary(hModule);
                
                return wtInfoProc != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        #region Native Interop

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        #endregion
    }
}
