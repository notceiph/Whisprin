// Copyright (c) Artisense. All rights reserved.

using System;
using Artisense.Core.AudioService;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Artisense.Tests.AudioService
{
    /// <summary>
    /// Unit tests for the <see cref="WasapiAudioService"/> class.
    /// </summary>
    public class WasapiAudioServiceTests
    {
        private readonly Mock<ILogger<WasapiAudioService>> mockLogger;

        public WasapiAudioServiceTests()
        {
            mockLogger = new Mock<ILogger<WasapiAudioService>>();
        }

        [Fact]
        public void Constructor_ValidLogger_InitializesCorrectly()
        {
            // Act
            var service = new WasapiAudioService(mockLogger.Object);

            // Assert
            service.Should().NotBeNull();
            service.IsPlaying.Should().BeFalse();
            service.VolumeOffsetDb.Should().Be(0.0f);
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new WasapiAudioService(null!);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
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
            var service = new WasapiAudioService(mockLogger.Object);

            // Act
            service.VolumeOffsetDb = input;

            // Assert
            service.VolumeOffsetDb.Should().Be(expected);
        }

        [Fact]
        public void Start_ValidPressure_DoesNotThrow()
        {
            // Arrange
            var service = new WasapiAudioService(mockLogger.Object);

            // Act & Assert
            // Note: This test may fail in CI due to no audio devices
            // In practice, we'd use dependency injection to mock the audio output
            var action = () => service.Start(0.5f);
            action.Should().NotThrow();
        }

        [Fact]
        public void SetPressure_ValidPressure_DoesNotThrow()
        {
            // Arrange
            var service = new WasapiAudioService(mockLogger.Object);

            // Act & Assert
            var action = () => service.SetPressure(0.7f);
            action.Should().NotThrow();
        }

        [Fact]
        public void Stop_WhenNotPlaying_DoesNotThrow()
        {
            // Arrange
            var service = new WasapiAudioService(mockLogger.Object);

            // Act & Assert
            var action = () => service.Stop();
            action.Should().NotThrow();
        }

        [Fact]
        public void Dispose_WhenCalled_DoesNotThrow()
        {
            // Arrange
            var service = new WasapiAudioService(mockLogger.Object);

            // Act & Assert
            var action = () => service.Dispose();
            action.Should().NotThrow();
        }

        [Fact]
        public void Start_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var service = new WasapiAudioService(mockLogger.Object);
            service.Dispose();

            // Act & Assert
            var action = () => service.Start(0.5f);
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void SetPressure_AfterDispose_DoesNotThrow()
        {
            // Arrange
            var service = new WasapiAudioService(mockLogger.Object);
            service.Dispose();

            // Act & Assert
            // SetPressure should gracefully handle disposed state
            var action = () => service.SetPressure(0.5f);
            action.Should().NotThrow();
        }
    }
}
