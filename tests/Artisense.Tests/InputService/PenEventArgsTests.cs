// Copyright (c) Artisense. All rights reserved.

using Artisense.Core.InputService;
using FluentAssertions;
using Xunit;

namespace Artisense.Tests.InputService
{
    /// <summary>
    /// Unit tests for the <see cref="PenEventArgs"/> class.
    /// </summary>
    public class PenEventArgsTests
    {
        [Theory]
        [InlineData(0.0f, 0.0f)]
        [InlineData(0.5f, 0.5f)]
        [InlineData(1.0f, 1.0f)]
        public void Constructor_ValidPressureValues_SetsPressureCorrectly(float input, float expected)
        {
            // Act
            var eventArgs = new PenEventArgs(input);

            // Assert
            eventArgs.Pressure.Should().Be(expected);
        }

        [Theory]
        [InlineData(-0.1f, 0.0f)]
        [InlineData(-1.0f, 0.0f)]
        [InlineData(1.1f, 1.0f)]
        [InlineData(2.0f, 1.0f)]
        public void Constructor_OutOfRangePressureValues_ClampsToPressureRange(float input, float expected)
        {
            // Act
            var eventArgs = new PenEventArgs(input);

            // Assert
            eventArgs.Pressure.Should().Be(expected);
        }

        [Fact]
        public void Constructor_NaNPressure_ClampsToZero()
        {
            // Act
            var eventArgs = new PenEventArgs(float.NaN);

            // Assert
            eventArgs.Pressure.Should().Be(0.0f);
        }

        [Theory]
        [InlineData(float.PositiveInfinity, 1.0f)]
        [InlineData(float.NegativeInfinity, 0.0f)]
        public void Constructor_InfinitePressure_ClampsToRange(float input, float expected)
        {
            // Act
            var eventArgs = new PenEventArgs(input);

            // Assert
            eventArgs.Pressure.Should().Be(expected);
        }
    }
}
