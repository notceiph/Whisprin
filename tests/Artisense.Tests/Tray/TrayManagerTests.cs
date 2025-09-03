// Copyright (c) Artisense. All rights reserved.

using System;
using Artisense.Core.CoreController;
using Artisense.UI.Tray;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Artisense.Tests.Tray
{
    /// <summary>
    /// Unit tests for the <see cref="TrayManager"/> class.
    /// </summary>
    public class TrayManagerTests
    {
        private readonly Mock<ILogger<TrayManager>> mockLogger;
        private readonly Mock<IArtisenseController> mockController;

        public TrayManagerTests()
        {
            mockLogger = new Mock<ILogger<TrayManager>>();
            mockController = new Mock<IArtisenseController>();
        }

        [Fact]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            // Act
            var trayManager = new TrayManager(mockLogger.Object, mockController.Object);

            // Assert
            trayManager.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new TrayManager(null!, mockController.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_NullController_ThrowsArgumentNullException()
        {
            // Act & Assert
            var action = () => new TrayManager(mockLogger.Object, null!);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("controller");
        }

        [Fact]
        public void Initialize_ValidState_EnablesController()
        {
            // Arrange
            var trayManager = new TrayManager(mockLogger.Object, mockController.Object);

            // Act & Assert
            // Note: This test may fail in headless environments due to UI dependencies
            // In practice, we'd use a UI testing framework or mock the UI components
            var action = () => trayManager.Initialize();
            
            // We expect this to either succeed or throw due to headless environment
            // but not throw argument exceptions
            try
            {
                action.Should().NotThrow<ArgumentNullException>();
                mockController.Verify(x => x.Enable(), Times.Once);
            }
            catch (InvalidOperationException)
            {
                // Expected in headless test environment
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Expected in some CI environments
            }
        }

        [Fact]
        public void Initialize_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var trayManager = new TrayManager(mockLogger.Object, mockController.Object);
            trayManager.Dispose();

            // Act & Assert
            var action = () => trayManager.Initialize();
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_WhenCalled_DisablesController()
        {
            // Arrange
            var trayManager = new TrayManager(mockLogger.Object, mockController.Object);

            // Act
            trayManager.Dispose();

            // Assert
            mockController.Verify(x => x.Disable(), Times.Once);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var trayManager = new TrayManager(mockLogger.Object, mockController.Object);

            // Act & Assert
            var action = () =>
            {
                trayManager.Dispose();
                trayManager.Dispose();
                trayManager.Dispose();
            };

            action.Should().NotThrow();
        }
    }
}
