using System;
using ImageGallery.Services;
using Xunit;

namespace ImageGallery.Tests.Services;

public class ZoomControllerTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultZoomLevel()
    {
        // Arrange & Act
        var controller = new ZoomController();

        // Assert
        Assert.Equal(1.0, controller.ZoomLevel);
    }

    [Fact]
    public void Constructor_InitializesAsNotZoomed()
    {
        // Arrange & Act
        var controller = new ZoomController();

        // Assert
        Assert.False(controller.IsZoomed);
    }

    [Fact]
    public void ZoomIn_IncreasesZoomLevel()
    {
        // Arrange
        var controller = new ZoomController();
        var initialZoom = controller.ZoomLevel;

        // Act
        controller.ZoomIn();

        // Assert
        Assert.True(controller.ZoomLevel > initialZoom);
    }

    [Fact]
    public void ZoomOut_DecreasesZoomLevel()
    {
        // Arrange
        var controller = new ZoomController();
        controller.ZoomIn(); // Zoom in first
        controller.ZoomIn(); // Zoom in again
        var beforeZoomOut = controller.ZoomLevel;

        // Act
        controller.ZoomOut();

        // Assert
        Assert.True(controller.ZoomLevel < beforeZoomOut);
    }

    [Fact]
    public void ZoomOut_DoesNotGoBelowOne()
    {
        // Arrange
        var controller = new ZoomController();

        // Act - try to zoom out many times
        for (var i = 0; i < 20; i++)
        {
            controller.ZoomOut();
        }

        // Assert
        Assert.Equal(1.0, controller.ZoomLevel);
    }

    [Fact]
    public void IsZoomed_TrueWhenZoomLevelGreaterThanOne()
    {
        // Arrange
        var controller = new ZoomController();

        // Act
        controller.ZoomIn();

        // Assert
        Assert.True(controller.IsZoomed);
    }

    [Fact]
    public void IsZoomed_FalseWhenZoomLevelIsOne()
    {
        // Arrange
        var controller = new ZoomController();
        controller.ZoomIn();

        // Act
        controller.ZoomOut();

        // Assert
        Assert.False(controller.IsZoomed);
    }

    [Fact]
    public void ResetZoom_SetsZoomLevelToOne()
    {
        // Arrange
        var controller = new ZoomController();
        controller.ZoomIn();
        controller.ZoomIn();
        controller.ZoomIn();

        // Act
        controller.ResetZoom();

        // Assert
        Assert.Equal(1.0, controller.ZoomLevel);
    }

    [Fact]
    public void ZoomChanged_EventFiresOnZoomIn()
    {
        // Arrange
        var controller = new ZoomController();
        double? newZoom = null;
        controller.ZoomChanged += (zoom) => newZoom = zoom;

        // Act
        controller.ZoomIn();

        // Assert
        Assert.NotNull(newZoom);
        Assert.Equal(controller.ZoomLevel, newZoom.Value);
    }

    [Fact]
    public void ZoomChanged_EventFiresOnZoomOut()
    {
        // Arrange
        var controller = new ZoomController();
        controller.ZoomIn();
        double? newZoom = null;
        controller.ZoomChanged += (zoom) => newZoom = zoom;

        // Act
        controller.ZoomOut();

        // Assert
        Assert.NotNull(newZoom);
        Assert.Equal(controller.ZoomLevel, newZoom.Value);
    }

    [Fact]
    public void ZoomedIn_EventFiresWhenZoomingIn()
    {
        // Arrange
        var controller = new ZoomController();
        var eventFired = false;
        controller.ZoomedIn += () => eventFired = true;

        // Act
        controller.ZoomIn();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void ZoomedOut_EventFiresWhenZoomingOut()
    {
        // Arrange
        var controller = new ZoomController();
        controller.ZoomIn(); // Zoom in first
        var eventFired = false;
        controller.ZoomedOut += () => eventFired = true;

        // Act
        controller.ZoomOut();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void ResetZoom_FiresZoomChangedEvent()
    {
        // Arrange
        var controller = new ZoomController();
        controller.ZoomIn();
        double? newZoom = null;
        controller.ZoomChanged += (zoom) => newZoom = zoom;

        // Act
        controller.ResetZoom();

        // Assert
        Assert.NotNull(newZoom);
        Assert.Equal(1.0, newZoom.Value);
    }
}
