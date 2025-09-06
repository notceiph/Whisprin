// Copyright (c) Artisense. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Artisense.Core.InputService
{
    /// <summary>
    /// Hosted service that manages pen input providers and provides unified pen input events.
    /// </summary>
    public class PenInputService : BackgroundService
    {
        private readonly ILogger<PenInputService> logger;
        private readonly IPenInputProvider[] providers;
        private IPenInputProvider? activeProvider;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PenInputService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="rawInputProvider">The raw input provider.</param>
        /// <param name="wintabProvider">The Wintab provider.</param>
        /// <param name="windowsInkProvider">The Windows Ink provider.</param>
        /// <param name="globalHookProvider">The global hook provider.</param>
        public PenInputService(
            ILogger<PenInputService> logger,
            RawInputPenProvider rawInputProvider,
            WintabPenProvider wintabProvider,
            WindowsInkPenProvider windowsInkProvider,
            GlobalHookPenProvider globalHookProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Order providers by preference (Windows Ink first for Wacom tablets)
            providers = new IPenInputProvider[]
            {
                windowsInkProvider ?? throw new ArgumentNullException(nameof(windowsInkProvider)),
                rawInputProvider ?? throw new ArgumentNullException(nameof(rawInputProvider)),
                globalHookProvider ?? throw new ArgumentNullException(nameof(globalHookProvider)),
                wintabProvider ?? throw new ArgumentNullException(nameof(wintabProvider))
            };
        }

        /// <summary>
        /// Occurs when the pen makes contact with the screen.
        /// </summary>
        public event EventHandler<PenEventArgs>? PenDown;

        /// <summary>
        /// Occurs when the pen moves while in contact with the screen.
        /// </summary>
        public event EventHandler<PenEventArgs>? PenMove;

        /// <summary>
        /// Occurs when the pen is lifted from the screen.
        /// </summary>
        public event EventHandler? PenUp;

        /// <summary>
        /// Gets a value indicating whether the service is currently active and listening for input.
        /// </summary>
        public bool IsActive => activeProvider?.IsActive ?? false;

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Starting pen input service");

            try
            {
                // Try to start providers in order of preference
                foreach (var provider in providers)
                {
                    if (provider.Start())
                    {
                        activeProvider = provider;
                        SubscribeToEvents(provider);
                        logger.LogInformation("✅ Started pen input provider: {ProviderType}", provider.GetType().Name);
                        Console.WriteLine($"✅ Active pen provider: {provider.GetType().Name}");
                        break;
                    }
                }

                if (activeProvider == null)
                {
                    logger.LogError("❌ Failed to start any pen input provider");
                    Console.WriteLine("❌ No pen input providers could be started!");
                    return;
                }

                // Keep the service running
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                logger.LogInformation("Pen input service stopping");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in pen input service");
            }
            finally
            {
                if (activeProvider != null)
                {
                    UnsubscribeFromEvents(activeProvider);
                    activeProvider.Stop();
                    activeProvider = null;
                }

                logger.LogInformation("Pen input service stopped");
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            if (activeProvider != null)
            {
                UnsubscribeFromEvents(activeProvider);
                activeProvider.Stop();
            }

            foreach (var provider in providers)
            {
                provider?.Dispose();
            }

            isDisposed = true;
            base.Dispose();
        }

        private void SubscribeToEvents(IPenInputProvider provider)
        {
            provider.PenDown += OnPenDown;
            provider.PenMove += OnPenMove;
            provider.PenUp += OnPenUp;
        }

        private void UnsubscribeFromEvents(IPenInputProvider provider)
        {
            provider.PenDown -= OnPenDown;
            provider.PenMove -= OnPenMove;
            provider.PenUp -= OnPenUp;
        }

        private void OnPenDown(object? sender, PenEventArgs e)
        {
            logger.LogDebug("Pen down: pressure={Pressure:F3}", e.Pressure);
            PenDown?.Invoke(this, e);
        }

        private void OnPenMove(object? sender, PenEventArgs e)
        {
            logger.LogDebug("Pen move: pressure={Pressure:F3}", e.Pressure);
            PenMove?.Invoke(this, e);
        }

        private void OnPenUp(object? sender, EventArgs e)
        {
            logger.LogDebug("Pen up");
            PenUp?.Invoke(this, e);
        }
    }
}
