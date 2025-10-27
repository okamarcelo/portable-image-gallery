using System;
using ImageGallery.Services;
using Xunit;

namespace ImageGallery.Tests.Services;

public class SlideshowControllerTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultInterval()
    {
        // Arrange & Act
        var controller = new SlideshowController();

        // Assert
        Assert.Equal(5.0, controller.IntervalSeconds);
    }

    [Fact]
    public void Constructor_InitializesAsNotRunning()
    {
        // Arrange & Act
        var controller = new SlideshowController();

        // Assert
        Assert.False(controller.IsRunning);
    }

    [Fact]
    public void Start_ChangesIsRunningToTrue()
    {
        // Arrange
        var controller = new SlideshowController();

        // Act
        controller.Start();

        // Assert
        Assert.True(controller.IsRunning);
    }

    [Fact]
    public void Stop_ChangesIsRunningToFalse()
    {
        // Arrange
        var controller = new SlideshowController();
        controller.Start();

        // Act
        controller.Stop();

        // Assert
        Assert.False(controller.IsRunning);
    }

    [Fact]
    public void IncreaseSpeed_IncreasesIntervalByHalfSecond()
    {
        // Arrange
        var controller = new SlideshowController();
        var initialInterval = controller.IntervalSeconds;

        // Act
        controller.IncreaseSpeed();

        // Assert
        Assert.Equal(initialInterval + 0.5, controller.IntervalSeconds);
    }

    [Fact]
    public void DecreaseSpeed_DecreasesIntervalByHalfSecond()
    {
        // Arrange
        var controller = new SlideshowController();
        controller.IncreaseSpeed(); // Set to 5.5
        var beforeDecrease = controller.IntervalSeconds;

        // Act
        controller.DecreaseSpeed();

        // Assert
        Assert.Equal(beforeDecrease - 0.5, controller.IntervalSeconds);
    }

    [Fact]
    public void DecreaseSpeed_DoesNotGoBelow0_5Seconds()
    {
        // Arrange
        var controller = new SlideshowController();
        
        // Act - decrease many times to try to go below 0.5
        for (var i = 0; i < 20; i++)
        {
            controller.DecreaseSpeed();
        }

        // Assert
        Assert.True(controller.IntervalSeconds >= 0.5);
    }

    [Fact]
    public void IntervalChanged_EventFiresOnIncreaseSpeed()
    {
        // Arrange
        var controller = new SlideshowController();
        double? newInterval = null;
        controller.IntervalChanged += (interval) => newInterval = interval;

        // Act
        controller.IncreaseSpeed();

        // Assert
        Assert.NotNull(newInterval);
        Assert.Equal(controller.IntervalSeconds, newInterval.Value);
    }

    [Fact]
    public void IntervalChanged_EventFiresOnDecreaseSpeed()
    {
        // Arrange
        var controller = new SlideshowController();
        controller.IncreaseSpeed(); // Set to 5.5 first
        double? newInterval = null;
        controller.IntervalChanged += (interval) => newInterval = interval;

        // Act
        controller.DecreaseSpeed();

        // Assert
        Assert.NotNull(newInterval);
        Assert.Equal(controller.IntervalSeconds, newInterval.Value);
    }

    [Fact]
    public void LogMessage_EventFiresOnStart()
    {
        // Arrange
        var controller = new SlideshowController();
        string? logMessage = null;
        controller.LogMessage += (msg) => logMessage = msg;

        // Act
        controller.Start();

        // Assert
        Assert.NotNull(logMessage);
        Assert.Contains("started", logMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LogMessage_EventFiresOnStop()
    {
        // Arrange
        var controller = new SlideshowController();
        string? logMessage = null;
        controller.LogMessage += (msg) => logMessage = msg;
        controller.Start();

        // Act
        controller.Stop();

        // Assert
        Assert.NotNull(logMessage);
        Assert.Contains("stopped", logMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Restart_StopsAndStartsTimer()
    {
        // Arrange
        var controller = new SlideshowController();
        controller.Start();

        // Act
        controller.Restart();

        // Assert
        Assert.True(controller.IsRunning);
    }
}
