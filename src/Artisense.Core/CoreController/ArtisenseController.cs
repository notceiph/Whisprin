// Copyright (c) Artisense. All rights reserved.

using System;
using Artisense.Core.AudioService;
using Artisense.Core.InputService;
using Microsoft.Extensions.Logging;

namespace Artisense.Core.CoreController
{
    /// <summary>
    /// Main controller that coordinates pen input events with audio output.
    /// </summary>
    public class ArtisenseController : IArtisenseController
    {
        private readonly ILogger<ArtisenseController> logger;
        private readonly PenInputService penInputService;
        private readonly IAudioService audioService;
        private bool isEnabled;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArtisenseController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="penInputService">The pen input service.</param>
        /// <param name="audioService">The audio service.</param>
        public ArtisenseController(
            ILogger<ArtisenseController> logger,
            PenInputService penInputService,
            IAudioService audioService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.penInputService = penInputService ?? throw new ArgumentNullException(nameof(penInputService));
            this.audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        }

        /// <inheritdoc/>
        public bool IsEnabled => isEnabled;

        /// <inheritdoc/>
        public float VolumeOffsetDb
        {
            get => audioService.VolumeOffsetDb;
            set => audioService.VolumeOffsetDb = value;
        }

        /// <inheritdoc/>
        public void Enable()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(ArtisenseController));
            }

            if (isEnabled)
            {
                return;
            }

            try
            {
                // Subscribe to pen input events
                penInputService.PenDown += OnPenDown;
                penInputService.PenMove += OnPenMove;
                penInputService.PenUp += OnPenUp;

                isEnabled = true;
                logger.LogInformation("Artisense controller enabled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to enable Artisense controller");
                Disable(); // Clean up on failure
                throw;
            }
        }

        /// <inheritdoc/>
        public void Disable()
        {
            if (!isEnabled)
            {
                return;
            }

            try
            {
                // Unsubscribe from pen input events
                penInputService.PenDown -= OnPenDown;
                penInputService.PenMove -= OnPenMove;
                penInputService.PenUp -= OnPenUp;

                // Stop any ongoing audio
                audioService.Stop();

                isEnabled = false;
                logger.LogInformation("Artisense controller disabled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disabling Artisense controller");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the controller and optionally releases the managed resources.
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
                Disable();
            }

            isDisposed = true;
        }

        private void OnPenDown(object? sender, PenEventArgs e)
        {
            if (!isEnabled)
            {
                return;
            }

            try
            {
                logger.LogDebug("Pen down - starting audio with pressure {Pressure:F3}", e.Pressure);
                audioService.Start(e.Pressure);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling pen down event");
            }
        }

        private void OnPenMove(object? sender, PenEventArgs e)
        {
            if (!isEnabled)
            {
                return;
            }

            try
            {
                logger.LogDebug("Pen move - updating pressure to {Pressure:F3}", e.Pressure);
                audioService.SetPressure(e.Pressure);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling pen move event");
            }
        }

        private void OnPenUp(object? sender, EventArgs e)
        {
            if (!isEnabled)
            {
                return;
            }

            try
            {
                logger.LogDebug("Pen up - stopping audio");
                audioService.Stop();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling pen up event");
            }
        }
    }
}
