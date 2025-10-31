using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using ImageGallery.Services;

namespace ImageGallery.Tests.Services
{
    public class ImageLoaderServiceTests
    {
        private ImageLoaderService CreateImageLoaderService()
        {
            var imageCache = new ImageCache(NullLogger<ImageCache>.Instance, 16, 8, 4);
            var imageManager = new ImageManager(NullLogger<ImageManager>.Instance, imageCache);
            var mosaicManager = new MosaicManager();
            var slideshowController = new SlideshowController();
            var pauseController = new PauseController();
            var zoomController = new ZoomController();
            var windowStateService = new WindowStateService(NullLogger<WindowStateService>.Instance, zoomController);
            
            return new ImageLoaderService(
                NullLogger<ImageLoaderService>.Instance,
                imageManager,
                mosaicManager,
                slideshowController,
                windowStateService);
        }

        [Fact]
        public async Task LoadFromCliArgumentsAsync_WithNullArgs_ReturnsEarly()
        {
            // Arrange
            var imageLoaderService = CreateImageLoaderService();

            // Act & Assert - Should not throw
            await imageLoaderService.LoadFromCliArgumentsAsync(null!);
        }

        [Fact]
        public async Task LoadFromCliArgumentsAsync_WithNonCliModeArgs_ReturnsEarly()
        {
            // Arrange
            var imageLoaderService = CreateImageLoaderService();
            var cliArgs = new CommandLineArguments(); // IsCliMode returns false when RootDirectory is null

            // Act & Assert - Should not throw
            await imageLoaderService.LoadFromCliArgumentsAsync(cliArgs);
        }

        [Fact]
        public void Constructor_InitializesSuccessfully()
        {
            // Act
            var imageLoaderService = CreateImageLoaderService();

            // Assert
            Assert.NotNull(imageLoaderService);
        }

        [Fact]
        public void Events_CanBeSubscribedTo()
        {
            // Arrange
            var imageLoaderService = CreateImageLoaderService();

            var loadingOverlayChanged = false;
            var loadingProgressStackChanged = false;
            var importProgressStackChanged = false;
            var loadingTextChanged = false;
            var shuffleRequested = false;
            var showImageRequested = false;
            var slideshowStartRequested = false;

            // Act - Subscribe to all events
            imageLoaderService.LoadingOverlayVisibilityChanged += _ => loadingOverlayChanged = true;
            imageLoaderService.LoadingProgressStackVisibilityChanged += _ => loadingProgressStackChanged = true;
            imageLoaderService.ImportProgressStackVisibilityChanged += _ => importProgressStackChanged = true;
            imageLoaderService.LoadingTextChanged += _ => loadingTextChanged = true;
            imageLoaderService.ShuffleImagesRequested += () => shuffleRequested = true;
            imageLoaderService.ShowImageRequested += _ => showImageRequested = true;
            imageLoaderService.SlideshowStartRequested += () => slideshowStartRequested = true;

            // Assert - All events should be subscribable without throwing exceptions
            Assert.False(loadingOverlayChanged); // Events haven't been triggered
            Assert.False(loadingProgressStackChanged);
            Assert.False(importProgressStackChanged);
            Assert.False(loadingTextChanged);
            Assert.False(shuffleRequested);
            Assert.False(showImageRequested);
            Assert.False(slideshowStartRequested);
        }

        [Fact]  
        public async Task ImportImagesAsync_CompletesWithoutError()
        {
            // Arrange
            var imageLoaderService = CreateImageLoaderService();

            var loadingOverlayVisibilityChanges = new List<Visibility>();
            imageLoaderService.LoadingOverlayVisibilityChanged += v => loadingOverlayVisibilityChanges.Add(v);

            // Act & Assert - Should not throw
            await imageLoaderService.ImportImagesAsync();
            
            // Should have at least triggered some UI updates
            Assert.True(loadingOverlayVisibilityChanges.Count > 0);
        }

        [Fact]
        public void CommandLineArguments_IsCliMode_ReturnsFalseForNullDirectory()
        {
            // Arrange
            var cliArgs = new CommandLineArguments();

            // Act & Assert
            Assert.False(cliArgs.IsCliMode);
        }

        [Fact]
        public void CommandLineArguments_IsCliMode_ReturnsTrueForValidDirectory()
        {
            // Arrange
            var cliArgs = new CommandLineArguments { RootDirectory = "C:\\TestDirectory" };

            // Act & Assert
            Assert.True(cliArgs.IsCliMode);
        }
    }
}