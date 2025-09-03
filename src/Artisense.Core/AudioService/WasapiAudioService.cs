// Copyright (c) Artisense. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace Artisense.Core.AudioService
{
    /// <summary>
    /// Audio service implementation using WASAPI exclusive mode for low-latency playback.
    /// </summary>
    public class WasapiAudioService : IAudioService
    {
        private readonly ILogger<WasapiAudioService> logger;
        private readonly Timer? idleTimer;
        private WasapiOut? audioOutput;
        private LoopStream? loopStream;
        private PressureVolumeProcessor? volumeProcessor;
        private bool isDisposed;
        private bool isPlaying;
        private float volumeOffsetDb;

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiAudioService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public WasapiAudioService(ILogger<WasapiAudioService> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            volumeOffsetDb = 0.0f;
            
            // Timer to dispose audio resources after 5 seconds of inactivity
            idleTimer = new Timer(OnIdleTimeout, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <inheritdoc/>
        public bool IsPlaying => isPlaying;

        /// <inheritdoc/>
        public float VolumeOffsetDb
        {
            get => volumeOffsetDb;
            set
            {
                volumeOffsetDb = Math.Clamp(value, -12.0f, 0.0f);
                if (volumeProcessor != null)
                {
                    volumeProcessor.VolumeOffsetDb = volumeOffsetDb;
                }
            }
        }

        /// <inheritdoc/>
        public void Start(float pressure)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(WasapiAudioService));
            }

            try
            {
                // Initialize audio output lazily
                if (audioOutput == null)
                {
                    InitializeAudioOutput();
                }

                if (audioOutput != null && volumeProcessor != null)
                {
                    volumeProcessor.SetPressure(pressure);
                    
                    if (!isPlaying)
                    {
                        audioOutput.Play();
                        isPlaying = true;
                        logger.LogDebug("Audio playback started with pressure {Pressure:F3}", pressure);
                    }
                }

                // Reset idle timer
                idleTimer?.Change(5000, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start audio playback");
            }
        }

        /// <inheritdoc/>
        public void SetPressure(float pressure)
        {
            if (isDisposed || !isPlaying)
            {
                return;
            }

            try
            {
                volumeProcessor?.SetPressure(pressure);
                
                // Reset idle timer
                idleTimer?.Change(5000, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to set pressure");
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!isPlaying)
            {
                return;
            }

            try
            {
                audioOutput?.Stop();
                isPlaying = false;
                logger.LogDebug("Audio playback stopped");
                
                // Start idle timer to dispose resources
                idleTimer?.Change(5000, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to stop audio playback");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the service and optionally releases the managed resources.
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
                DisposeAudioResources();
                idleTimer?.Dispose();
            }

            isDisposed = true;
        }

        private void InitializeAudioOutput()
        {
            try
            {
                // Load embedded pencil loop audio
                var audioStream = LoadEmbeddedAudio();
                if (audioStream == null)
                {
                    logger.LogError("Failed to load embedded audio asset");
                    return;
                }

                var audioFileReader = new WaveFileReader(audioStream);
                loopStream = new LoopStream(audioFileReader);
                
                // Convert to ISampleProvider for processing
                var sampleProvider = loopStream.ToSampleProvider();
                volumeProcessor = new PressureVolumeProcessor(sampleProvider)
                {
                    VolumeOffsetDb = volumeOffsetDb
                };

                // Try WASAPI exclusive mode first, fallback to shared mode
                try
                {
                    audioOutput = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Exclusive, 50);
                    logger.LogInformation("Audio initialized in exclusive mode");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Exclusive mode failed, trying shared mode");
                    audioOutput = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 100);
                    logger.LogInformation("Audio initialized in shared mode");
                }

                audioOutput.Init(volumeProcessor);
                
                logger.LogInformation("Audio output initialized successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize audio output");
                DisposeAudioResources();
            }
        }

        private Stream? LoadEmbeddedAudio()
        {
            try
            {
                // Try to load from UI assembly (where the resource is embedded)
                #pragma warning disable S6602 // Using FirstOrDefault for assembly search is appropriate here
                var uiAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Artisense.UI");
                #pragma warning restore S6602
                
                if (uiAssembly != null)
                {
                    var resourceName = "Artisense.UI.Assets.pencil_loop.wav";
                    var stream = uiAssembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        logger.LogInformation("Successfully loaded embedded audio resource");
                        return stream;
                    }
                }
                
                // Fallback: try current assembly
                var assembly = Assembly.GetExecutingAssembly();
                var fallbackResourceName = "Artisense.UI.Assets.pencil_loop.wav";
                var fallbackStream = assembly.GetManifestResourceStream(fallbackResourceName);
                
                if (fallbackStream != null)
                {
                    logger.LogInformation("Loaded audio resource from current assembly");
                    return fallbackStream;
                }
                
                logger.LogError("Failed to find embedded audio resource in any assembly");
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load embedded audio resource");
                return null;
            }
        }

        private void DisposeAudioResources()
        {
            try
            {
                audioOutput?.Dispose();
                loopStream?.Dispose();
                volumeProcessor = null;
                audioOutput = null;
                loopStream = null;
                isPlaying = false;
                
                logger.LogDebug("Audio resources disposed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disposing audio resources");
            }
        }

        private void OnIdleTimeout(object? state)
        {
            if (!isPlaying)
            {
                logger.LogDebug("Disposing audio resources due to inactivity");
                DisposeAudioResources();
            }
        }
    }
}
