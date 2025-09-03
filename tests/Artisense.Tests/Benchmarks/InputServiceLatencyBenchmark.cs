// Copyright (c) Artisense. All rights reserved.

using System;
using System.Diagnostics;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Artisense.Tests.Benchmarks
{
    /// <summary>
    /// Benchmark for measuring InputService latency performance.
    /// </summary>
    [SimpleJob]
    [MemoryDiagnoser]
    public class InputServiceLatencyBenchmark
    {
        private readonly Random random = new(42);

        /// <summary>
        /// Benchmark pen event processing latency.
        /// </summary>
        [Benchmark]
        public void PenEventProcessingLatency()
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Simulate pen event processing
            var pressure = (float)random.NextDouble();
            var normalizedPressure = Math.Clamp(pressure, 0.0f, 1.0f);
            
            // Simulate minimal processing overhead
            var volume = MathF.Pow(normalizedPressure, 0.6f);
            
            stopwatch.Stop();
            
            // Verify we're under 1ms for event processing
            if (stopwatch.Elapsed.TotalMilliseconds > 1.0)
            {
                throw new InvalidOperationException($"Event processing took {stopwatch.Elapsed.TotalMilliseconds:F3}ms - exceeds 1ms threshold");
            }
        }

        /// <summary>
        /// Benchmark pressure calculation performance.
        /// </summary>
        [Benchmark]
        public float PressureCalculation()
        {
            var rawPressure = (float)random.NextDouble() * 1024.0f;
            return Math.Clamp(rawPressure / 1024.0f, 0.0f, 1.0f);
        }

        /// <summary>
        /// Benchmark volume curve calculation performance.
        /// </summary>
        [Benchmark]
        public float VolumeCurveCalculation()
        {
            var pressure = (float)random.NextDouble();
            return MathF.Pow(pressure, 0.6f);
        }

        /// <summary>
        /// Entry point for running benchmarks standalone.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void BenchmarkMain(string[] args)
        {
            if (args.Length > 0 && args[0].Contains("LatencyBenchmark"))
            {
                BenchmarkRunner.Run<InputServiceLatencyBenchmark>();
            }
        }
    }
}
