using System;
using System.Windows;
using ImageGallery.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ImageGallery.Tests.Services;

public class WindowStateServiceTests
{
    [Fact]
    public void WindowStateService_Constructor_DoesNotThrow()
    {
        // Arrange & Act & Assert
        var logger = NullLogger<WindowStateService>.Instance;
        var zoomController = new ZoomController();

        var service = new WindowStateService(logger, zoomController);

        Assert.NotNull(service);
        Assert.False(service.IsFullscreen);
    }

    [Fact]
    public void Initialize_WithNullWindow_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateWindowStateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.Initialize(null!));
    }

    [Fact]
    public void IsNearEdge_WithoutInitialization_ReturnsFalse()
    {
        // Arrange
        var service = CreateWindowStateService();
        var point = new Point(10, 10);

        // Act
        var result = service.IsNearEdge(point);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFullscreen_InitiallyFalse()
    {
        // Arrange
        var service = CreateWindowStateService();

        // Act & Assert
        Assert.False(service.IsFullscreen);
    }

    [Fact]
    public void HandleMouseLeftButtonDown_WithoutInitialization_ReturnsFalse()
    {
        // Arrange
        var service = CreateWindowStateService();
        var point = new Point(10, 10);

        // Act
        var result = service.HandleMouseLeftButtonDown(point);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HandleMouseMove_WithoutInitialization_DoesNotThrow()
    {
        // Arrange
        var service = CreateWindowStateService();
        var point = new Point(10, 10);

        // Act & Assert - Should not throw
        service.HandleMouseMove(point);
    }

    [Fact]
    public void ToggleFullscreen_WithoutInitialization_DoesNotThrow()
    {
        // Arrange
        var service = CreateWindowStateService();

        // Act & Assert - Should not throw
        service.ToggleFullscreen();
        Assert.False(service.IsFullscreen); // Should remain false without window
    }

    [Fact]
    public void LogMessage_EventExists()
    {
        // Arrange
        var service = CreateWindowStateService();
        var logMessageReceived = false;

        // Act
        service.LogMessage += msg => logMessageReceived = true;

        // Assert - Event should be subscribable without throwing
        Assert.False(logMessageReceived); // No message sent yet
    }

    private WindowStateService CreateWindowStateService()
    {
        var logger = NullLogger<WindowStateService>.Instance;
        var zoomController = new ZoomController();
        return new WindowStateService(logger, zoomController);
    }


}