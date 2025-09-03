// Copyright (c) Artisense. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Artisense.Core.InputService;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Artisense.Tests.InputService
{
    /// <summary>
    /// Unit tests for the <see cref="PenInputService"/> class.
    /// </summary>
    public class PenInputServiceTests
    {
        private readonly Mock<ILogger<PenInputService>> mockLogger;
        private readonly Mock<RawInputPenProvider> mockRawInputProvider;
        private readonly Mock<WintabPenProvider> mockWintabProvider;

        public PenInputServiceTests()
        {
            mockLogger = new Mock<ILogger<PenInputService>>();
            
            var mockRawInputLogger = new Mock<ILogger<RawInputPenProvider>>();
            mockRawInputProvider = new Mock<RawInputPenProvider>(mockRawInputLogger.Object);
            
            var mockWintabLogger = new Mock<ILogger<WintabPenProvider>>();
            mockWintabProvider = new Mock<WintabPenProvider>(mockWintabLogger.Object);
        }

        [Fact]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            // Act
            var service = new PenInputService(
                mockLogger.Object,
                mockRawInputProvider.Object,
                mockWintabProvider.Object);

            // Assert
            service.Should().NotBeNull();
            service.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new PenInputService(
                null!,
                mockRawInputProvider.Object,
                mockWintabProvider.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_NullRawInputProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new PenInputService(
                mockLogger.Object,
                null!,
                mockWintabProvider.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("rawInputProvider");
        }

        [Fact]
        public void Constructor_NullWintabProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new PenInputService(
                mockLogger.Object,
                mockRawInputProvider.Object,
                null!);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("wintabProvider");
        }

        [Fact]
        public async Task StartAsync_RawInputProviderSucceeds_UsesRawInputProvider()
        {
            // Arrange
            mockRawInputProvider.Setup(x => x.Start()).Returns(true);
            mockRawInputProvider.Setup(x => x.IsActive).Returns(true);

            var service = new PenInputService(
                mockLogger.Object,
                mockRawInputProvider.Object,
                mockWintabProvider.Object);

            var cts = new CancellationTokenSource();

            // Act
            var task = service.StartAsync(cts.Token);
            await Task.Delay(100); // Allow service to start
            cts.Cancel();
            await task;

            // Assert
            mockRawInputProvider.Verify(x => x.Start(), Times.Once);
            mockWintabProvider.Verify(x => x.Start(), Times.Never);
        }

        [Fact]
        public async Task StartAsync_RawInputFallsBackToWintab_UsesWintabProvider()
        {
            // Arrange
            mockRawInputProvider.Setup(x => x.Start()).Returns(false);
            mockWintabProvider.Setup(x => x.Start()).Returns(true);
            mockWintabProvider.Setup(x => x.IsActive).Returns(true);

            var service = new PenInputService(
                mockLogger.Object,
                mockRawInputProvider.Object,
                mockWintabProvider.Object);

            var cts = new CancellationTokenSource();

            // Act
            var task = service.StartAsync(cts.Token);
            await Task.Delay(100); // Allow service to start
            cts.Cancel();
            await task;

            // Assert
            mockRawInputProvider.Verify(x => x.Start(), Times.Once);
            mockWintabProvider.Verify(x => x.Start(), Times.Once);
        }

        [Fact]
        public async Task PenEvents_WhenActiveProviderRaisesEvents_ForwardsEventsCorrectly()
        {
            // Arrange
            mockRawInputProvider.Setup(x => x.Start()).Returns(true);
            mockRawInputProvider.Setup(x => x.IsActive).Returns(true);

            var service = new PenInputService(
                mockLogger.Object,
                mockRawInputProvider.Object,
                mockWintabProvider.Object);

            var penDownEventReceived = false;
            var penMoveEventReceived = false;
            var penUpEventReceived = false;
            var receivedPressure = 0.0f;

            service.PenDown += (s, e) =>
            {
                penDownEventReceived = true;
                receivedPressure = e.Pressure;
            };

            service.PenMove += (s, e) =>
            {
                penMoveEventReceived = true;
                receivedPressure = e.Pressure;
            };

            service.PenUp += (s, e) => penUpEventReceived = true;

            var cts = new CancellationTokenSource();

            // Act
            var task = service.StartAsync(cts.Token);
            await Task.Delay(50); // Allow service to start

            // Simulate pen events from the provider
            var testPressure = 0.7f;
            mockRawInputProvider.Raise(x => x.PenDown += null, new PenEventArgs(testPressure));
            mockRawInputProvider.Raise(x => x.PenMove += null, new PenEventArgs(testPressure + 0.1f));
            mockRawInputProvider.Raise(x => x.PenUp += null, EventArgs.Empty);

            cts.Cancel();
            await task;

            // Assert
            penDownEventReceived.Should().BeTrue();
            penMoveEventReceived.Should().BeTrue();
            penUpEventReceived.Should().BeTrue();
            receivedPressure.Should().Be(testPressure + 0.1f);
        }

        [Fact]
        public async Task StopAsync_WhenRunning_StopsActiveProvider()
        {
            // Arrange
            mockRawInputProvider.Setup(x => x.Start()).Returns(true);
            mockRawInputProvider.Setup(x => x.IsActive).Returns(true);

            var service = new PenInputService(
                mockLogger.Object,
                mockRawInputProvider.Object,
                mockWintabProvider.Object);

            var cts = new CancellationTokenSource();

            // Act
            var task = service.StartAsync(cts.Token);
            await Task.Delay(50); // Allow service to start
            cts.Cancel();
            await task;

            // Assert
            mockRawInputProvider.Verify(x => x.Stop(), Times.Once);
        }
    }
}
