// Copyright (c) Artisense. All rights reserved.

using System;
using Artisense.Core.AudioService;
using Artisense.Core.CoreController;
using Artisense.Core.InputService;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Artisense.Tests.CoreController
{
    /// <summary>
    /// Unit tests for the <see cref="ArtisenseController"/> class.
    /// </summary>
    public class ArtisenseControllerTests
    {
        private readonly Mock<ILogger<ArtisenseController>> mockLogger;
        private readonly Mock<PenInputService> mockPenInputService;
        private readonly Mock<IAudioService> mockAudioService;

        public ArtisenseControllerTests()
        {
            mockLogger = new Mock<ILogger<ArtisenseController>>();
            
            var mockPenLogger = new Mock<ILogger<PenInputService>>();
            var mockRawInputProvider = new Mock<RawInputPenProvider>(Mock.Of<ILogger<RawInputPenProvider>>());
            var mockWintabProvider = new Mock<WintabPenProvider>(Mock.Of<ILogger<WintabPenProvider>>());
            
            mockPenInputService = new Mock<PenInputService>(
                mockPenLogger.Object,
                mockRawInputProvider.Object,
                mockWintabProvider.Object);
            
            mockAudioService = new Mock<IAudioService>();
        }

        [Fact]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            // Act
            var controller = new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                mockAudioService.Object);

            // Assert
            controller.Should().NotBeNull();
            controller.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new ArtisenseController(
                null!,
                mockPenInputService.Object,
                mockAudioService.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_NullPenInputService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new ArtisenseController(
                mockLogger.Object,
                null!,
                mockAudioService.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("penInputService");
        }

        [Fact]
        public void Constructor_NullAudioService_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                null!);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("audioService");
        }

        [Fact]
        public void Enable_WhenDisabled_EnablesController()
        {
            // Arrange
            var controller = new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                mockAudioService.Object);

            // Act
            controller.Enable();

            // Assert
            controller.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void Enable_WhenAlreadyEnabled_DoesNotThrow()
        {
            // Arrange
            var controller = new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                mockAudioService.Object);

            controller.Enable();

            // Act & Assert
            var action = () => controller.Enable();
            action.Should().NotThrow();
        }

        [Fact]
        public void Disable_WhenEnabled_DisablesController()
        {
            // Arrange
            var controller = new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                mockAudioService.Object);

            controller.Enable();

            // Act
            controller.Disable();

            // Assert
            controller.IsEnabled.Should().BeFalse();
            mockAudioService.Verify(x => x.Stop(), Times.Once);
        }

        [Fact]
        public void Disable_WhenAlreadyDisabled_DoesNotThrow()
        {
            // Arrange
            var controller = new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                mockAudioService.Object);

            // Act & Assert
            var action = () => controller.Disable();
            action.Should().NotThrow();
        }

        [Fact]
        public void VolumeOffsetDb_GetSet_DelegatesToAudioService()
        {
            // Arrange
            var controller = new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                mockAudioService.Object);

            var testValue = -6.0f;
            mockAudioService.SetupProperty(x => x.VolumeOffsetDb);

            // Act
            controller.VolumeOffsetDb = testValue;
            var result = controller.VolumeOffsetDb;

            // Assert
            mockAudioService.VerifySet(x => x.VolumeOffsetDb = testValue, Times.Once);
            mockAudioService.VerifyGet(x => x.VolumeOffsetDb, Times.Once);
        }

        [Fact]
        public void PenDownEvent_WhenEnabled_StartsAudio()
        {
            // Arrange
            var controller = new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                mockAudioService.Object);

            controller.Enable();
            var testPressure = 0.7f;

            // Act
            mockPenInputService.Raise(x => x.PenDown += null, new PenEventArgs(testPressure));

            // Assert
            mockAudioService.Verify(x => x.Start(testPressure), Times.Once);
        }

        [Fact]
        public void PenMoveEvent_WhenEnabled_UpdatesPressure()
        {
            // Arrange
            var controller = new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                mockAudioService.Object);

            controller.Enable();
            var testPressure = 0.5f;

            // Act
            mockPenInputService.Raise(x => x.PenMove += null, new PenEventArgs(testPressure));

            // Assert
            mockAudioService.Verify(x => x.SetPressure(testPressure), Times.Once);
        }

        [Fact]
        public void PenUpEvent_WhenEnabled_StopsAudio()
        {
            // Arrange
            var controller = new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                mockAudioService.Object);

            controller.Enable();

            // Act
            mockPenInputService.Raise(x => x.PenUp += null, EventArgs.Empty);

            // Assert
            mockAudioService.Verify(x => x.Stop(), Times.Once);
        }

        [Fact]
        public void PenEvents_WhenDisabled_DoNotTriggerAudio()
        {
            // Arrange
            var controller = new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                mockAudioService.Object);

            // Don't enable the controller

            // Act
            mockPenInputService.Raise(x => x.PenDown += null, new PenEventArgs(0.5f));
            mockPenInputService.Raise(x => x.PenMove += null, new PenEventArgs(0.7f));
            mockPenInputService.Raise(x => x.PenUp += null, EventArgs.Empty);

            // Assert
            mockAudioService.Verify(x => x.Start(It.IsAny<float>()), Times.Never);
            mockAudioService.Verify(x => x.SetPressure(It.IsAny<float>()), Times.Never);
            mockAudioService.Verify(x => x.Stop(), Times.Never);
        }

        [Fact]
        public void Enable_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var controller = new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                mockAudioService.Object);

            controller.Dispose();

            // Act & Assert
            var action = () => controller.Enable();
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_WhenEnabled_DisablesController()
        {
            // Arrange
            var controller = new ArtisenseController(
                mockLogger.Object,
                mockPenInputService.Object,
                mockAudioService.Object);

            controller.Enable();

            // Act
            controller.Dispose();

            // Assert
            controller.IsEnabled.Should().BeFalse();
            mockAudioService.Verify(x => x.Stop(), Times.Once);
        }
    }
}
