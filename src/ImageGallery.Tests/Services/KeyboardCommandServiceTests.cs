using System;
using ImageGallery.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ImageGallery.Tests.Services;

public class KeyboardCommandServiceTests
{
    [Fact]
    public void KeyboardCommandService_Constructor_DoesNotThrow()
    {
        // Arrange & Act & Assert - testing that the service can be constructed
        var logger = NullLogger<KeyboardCommandService>.Instance;
        var imageManager = new ImageManager(logger: NullLogger<ImageManager>.Instance, imageCache: null!);
        var zoomController = new ZoomController();
        var mosaicManager = new MosaicManager();
        var slideshowController = new SlideshowController();
        var pauseController = new PauseController();
        var debugLogger = new DebugLogger();

        var service = new KeyboardCommandService(
            logger,
            imageManager,
            zoomController,
            mosaicManager,
            slideshowController,
            pauseController,
            debugLogger);

        Assert.NotNull(service);
    }

    [Fact]
    public void KeyboardCommandService_EventsExist()
    {
        // Arrange
        var service = CreateKeyboardCommandService();
        var eventsFired = 0;

        // Act - Subscribe to events to verify they exist
        service.NavigateNextRequested += () => eventsFired++;
        service.NavigatePreviousRequested += () => eventsFired++;
        service.ToggleFullscreenRequested += () => eventsFired++;
        service.SelectDirectoryRequested += () => eventsFired++;

        // Assert - Events should be subscribable
        Assert.Equal(0, eventsFired); // No events fired yet, but no exceptions thrown
    }

    private KeyboardCommandService CreateKeyboardCommandService()
    {
        var logger = NullLogger<KeyboardCommandService>.Instance;
        var imageManager = new ImageManager(logger: NullLogger<ImageManager>.Instance, imageCache: null!);
        var zoomController = new ZoomController();
        var mosaicManager = new MosaicManager();
        var slideshowController = new SlideshowController();
        var pauseController = new PauseController();
        var debugLogger = new DebugLogger();

        return new KeyboardCommandService(
            logger,
            imageManager,
            zoomController,
            mosaicManager,
            slideshowController,
            pauseController,
            debugLogger);
    }
}