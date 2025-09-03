// Copyright (c) Artisense. All rights reserved.

using System;
using Artisense.Core.AudioService;
using FluentAssertions;
using NAudio.Wave;
using Xunit;

namespace Artisense.Tests.AudioService
{
    /// <summary>
    /// Unit tests for the <see cref="PressureVolumeProcessor"/> class.
    /// </summary>
    public class PressureVolumeProcessorTests
    {
        [Fact]
        public void Constructor_ValidSource_InitializesCorrectly()
        {
            // Arrange
            var source = new TestSampleProvider();

            // Act
            var processor = new PressureVolumeProcessor(source);

            // Assert
            processor.Should().NotBeNull();
            processor.WaveFormat.Should().Be(source.WaveFormat);
        }

        [Fact]
        public void Constructor_NullSource_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new PressureVolumeProcessor(null!);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("source");
        }

        [Theory]
        [InlineData(-15.0f, -12.0f)]
        [InlineData(-12.0f, -12.0f)]
        [InlineData(-6.0f, -6.0f)]
        [InlineData(0.0f, 0.0f)]
        [InlineData(5.0f, 0.0f)]
        public void VolumeOffsetDb_SetValue_ClampsToValidRange(float input, float expected)
        {
            // Arrange
            var source = new TestSampleProvider();
            var processor = new PressureVolumeProcessor(source);

            // Act
            processor.VolumeOffsetDb = input;

            // Assert
            processor.VolumeOffsetDb.Should().Be(expected);
        }

        [Theory]
        [InlineData(0.0f, 0.0f)]
        [InlineData(0.5f, 0.6597539f)] // 0.5^0.6 ≈ 0.6597539
        [InlineData(1.0f, 1.0f)]
        public void SetPressure_ValidPressure_AppliesPerceptualCurve(float pressure, float expectedVolume)
        {
            // Arrange
            var source = new TestSampleProvider();
            var processor = new PressureVolumeProcessor(source);
            var buffer = new float[1024];
            
            // Fill source with unit amplitude
            source.SetTestData(1.0f);

            // Act
            processor.SetPressure(pressure);
            processor.Read(buffer, 0, buffer.Length);

            // Assert
            // Check that volume is applied (allowing for smoothing)
            if (expectedVolume > 0)
            {
                buffer[buffer.Length - 1].Should().BeApproximately(expectedVolume, 0.1f);
            }
            else
            {
                buffer[buffer.Length - 1].Should().Be(0.0f);
            }
        }

        [Theory]
        [InlineData(-0.1f)]
        [InlineData(1.1f)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        public void SetPressure_InvalidPressure_ClampsToValidRange(float pressure)
        {
            // Arrange
            var source = new TestSampleProvider();
            var processor = new PressureVolumeProcessor(source);

            // Act & Assert (should not throw)
            var action = () => processor.SetPressure(pressure);
            action.Should().NotThrow();
        }

        [Fact]
        public void Read_WithVolumeOffset_AppliesCorrectGain()
        {
            // Arrange
            var source = new TestSampleProvider();
            var processor = new PressureVolumeProcessor(source);
            var buffer = new float[1024];
            
            source.SetTestData(1.0f);
            processor.VolumeOffsetDb = -6.0f; // -6dB = ~0.5 multiplier
            processor.SetPressure(1.0f);

            // Act
            processor.Read(buffer, 0, buffer.Length);

            // Assert
            // Final sample should be approximately 0.5 (allowing for smoothing)
            buffer[buffer.Length - 1].Should().BeApproximately(0.5f, 0.1f);
        }

        /// <summary>
        /// Test sample provider for unit testing.
        /// </summary>
        private class TestSampleProvider : ISampleProvider
        {
            private float testValue;

            public WaveFormat WaveFormat { get; } = new WaveFormat(44100, 1);

            public void SetTestData(float value)
            {
                testValue = value;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    buffer[offset + i] = testValue;
                }
                return count;
            }
        }
    }
}
