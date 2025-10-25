using System;
using ImageGallery.Services;
using Xunit;

namespace ImageGallery.Tests.Services;

public class PauseControllerTests
{
    [Fact]
    public void Constructor_InitializesPausedStateAsFalse()
    {
        // Arrange & Act
        var controller = new PauseController();

        // Assert
        Assert.False(controller.IsPaused);
    }

    [Fact]
    public void Toggle_ChangesPauseState()
    {
        // Arrange
        var controller = new PauseController();
        var pausedEventFired = false;
        controller.Paused += () => pausedEventFired = true;

        // Act
        controller.Toggle();

        // Assert
        Assert.True(controller.IsPaused);
        Assert.True(pausedEventFired);
    }

    [Fact]
    public void Toggle_TwiceReturnsToPreviousState()
    {
        // Arrange
        var controller = new PauseController();
        var resumedEventFired = false;
        controller.Resumed += () => resumedEventFired = true;

        // Act
        controller.Toggle(); // Pause
        controller.Toggle(); // Resume

        // Assert
        Assert.False(controller.IsPaused);
        Assert.True(resumedEventFired);
    }

    [Fact]
    public void Pause_SetsPausedStateToTrue()
    {
        // Arrange
        var controller = new PauseController();

        // Act
        controller.Pause();

        // Assert
        Assert.True(controller.IsPaused);
    }

    [Fact]
    public void Resume_SetsPausedStateToFalse()
    {
        // Arrange
        var controller = new PauseController();
        controller.Pause(); // First pause

        // Act
        controller.Resume();

        // Assert
        Assert.False(controller.IsPaused);
    }

    [Fact]
    public void Paused_EventFiresWhenPausing()
    {
        // Arrange
        var controller = new PauseController();
        var eventFired = false;
        controller.Paused += () => eventFired = true;

        // Act
        controller.Pause();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void Resumed_EventFiresWhenResuming()
    {
        // Arrange
        var controller = new PauseController();
        var eventFired = false;
        controller.Paused += () => { }; // Subscribe to prevent null
        controller.Resumed += () => eventFired = true;
        controller.Pause(); // First pause

        // Act
        controller.Resume();

        // Assert
        Assert.True(eventFired);
    }
}
