// Copyright (c) Artisense. All rights reserved.

using System;
using System.Diagnostics;
using Artisense.Core.AudioService;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using Moq;

namespace Artisense.Tests.Benchmarks
{
    /// <summary>
    /// Benchmark for measuring AudioService latency performance.
    /// </summary>
    [SimpleJob]
    [MemoryDiagnoser]
    public class AudioServiceLatencyBenchmark
    {
        private WasapiAudioService? audioService;
        private readonly Random random = new(42);

        /// <summary>
        /// Sets up the benchmark environment.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            var mockLogger = new Mock<ILogger<WasapiAudioService>>();
            audioService = new WasapiAudioService(mockLogger.Object);
        }

        /// <summary>
        /// Cleans up the benchmark environment.
        /// </summary>
        [GlobalCleanup]
        public void Cleanup()
        {
            audioService?.Dispose();
        }

        /// <summary>
        /// Benchmark audio start latency.
        /// </summary>
        [Benchmark]
        public void AudioStartLatency()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var pressure = (float)random.NextDouble();
                audioService?.Start(pressure);
                audioService?.Stop();
            }
            catch
            {
                // Ignore audio device errors in benchmarking
            }
            
            stopwatch.Stop();
            
            // Verify we're under 5ms for start operation
            if (stopwatch.Elapsed.TotalMilliseconds > 5.0)
            {
                Console.WriteLine($"Warning: Audio start took {stopwatch.Elapsed.TotalMilliseconds:F3}ms");
            }
        }

        /// <summary>
        /// Benchmark pressure update latency.
        /// </summary>
        [Benchmark]
        public void PressureUpdateLatency()
        {
            var stopwatch = Stopwatch.StartNew();
            
            var pressure = (float)random.NextDouble();
            audioService?.SetPressure(pressure);
            
            stopwatch.Stop();
            
            // Verify we're under 1ms for pressure updates
            if (stopwatch.Elapsed.TotalMilliseconds > 1.0)
            {
                throw new InvalidOperationException($"Pressure update took {stopwatch.Elapsed.TotalMilliseconds:F3}ms - exceeds 1ms threshold");
            }
        }

        /// <summary>
        /// Benchmark volume calculation performance.
        /// </summary>
        [Benchmark]
        public float VolumeCalculation()
        {
            var pressure = (float)random.NextDouble();
            var perceptualVolume = MathF.Pow(pressure, 0.6f);
            var offsetMultiplier = MathF.Pow(10.0f, -6.0f / 20.0f); // -6dB
            return perceptualVolume * offsetMultiplier;
        }

        /// <summary>
        /// Entry point for running benchmarks standalone.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Contains("AudioServiceLatencyBenchmark"))
            {
                BenchmarkRunner.Run<AudioServiceLatencyBenchmark>();
            }
        }
    }
}
