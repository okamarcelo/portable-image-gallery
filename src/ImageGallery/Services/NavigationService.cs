using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageGallery.Resources;

namespace ImageGallery.Services
{
    public class NavigationService
    {
        private readonly ILogger<NavigationService> _logger;
        private readonly ImageManager _imageManager;
        private readonly MosaicManager _mosaicManager;
        private readonly SlideshowController _slideshowController;
        private readonly PauseController _pauseController;
        private readonly DebugLogger _debugLogger;

        private int _currentIndex = 0;
        private bool _isAutomaticNavigation = false; // Track if navigation is from slideshow

        // Events for UI updates
        public event Action<List<BitmapImage>>? ImagesDisplayRequested;
        public event Action MosaicLayoutUpdateRequested;
        public event Action<bool>? FlashSideRequested; // true = right, false = left
        public event Action<string>? LogMessageRequested;
        public event Action? SlideTransitionRequested; // Request slide animation for automatic navigation

        public int CurrentIndex 
        { 
            get => _currentIndex; 
            set => _currentIndex = value; 
        }

        public NavigationService(
            ILogger<NavigationService> logger,
            ImageManager imageManager,
            MosaicManager mosaicManager,
            SlideshowController slideshowController,
            PauseController pauseController,
            DebugLogger debugLogger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _imageManager = imageManager ?? throw new ArgumentNullException(nameof(imageManager));
            _mosaicManager = mosaicManager ?? throw new ArgumentNullException(nameof(mosaicManager));
            _slideshowController = slideshowController ?? throw new ArgumentNullException(nameof(slideshowController));
            _pauseController = pauseController ?? throw new ArgumentNullException(nameof(pauseController));
            _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
        }

        public void NavigateNext()
        {
            _logger.LogTrace("Navigate Next: currentIndex={CurrentIndex}, imageCount={ImageCount}, paneCount={PaneCount}", 
                _currentIndex, _imageManager.ImageCount, _mosaicManager.PaneCount);
            _debugLogger.Log($"[NAV] Next pressed - currentIndex: {_currentIndex}, imageCount: {_imageManager.ImageCount}, paneCount: {_mosaicManager.PaneCount}");
            
            _slideshowController.Stop();
            _currentIndex = (_currentIndex + _mosaicManager.PaneCount) % _imageManager.ImageCount;
            
            _logger.LogTrace("New currentIndex after Next: {CurrentIndex}", _currentIndex);
            _debugLogger.Log($"[NAV] New currentIndex after Next: {_currentIndex}");
            
            _mosaicManager.ResetPaneIndex();
            _ = ShowImageAsync(_currentIndex);
            FlashSideRequested?.Invoke(true);
            
            if (!_pauseController.IsPaused)
                _slideshowController.Start();
        }

        public void NavigatePrevious()
        {
            _logger.LogTrace("Navigate Previous: currentIndex={CurrentIndex}, imageCount={ImageCount}, paneCount={PaneCount}", 
                _currentIndex, _imageManager.ImageCount, _mosaicManager.PaneCount);
            _debugLogger.Log($"[NAV] Previous pressed - currentIndex: {_currentIndex}, imageCount: {_imageManager.ImageCount}, paneCount: {_mosaicManager.PaneCount}");
            
            _slideshowController.Stop();
            _currentIndex = (_currentIndex - _mosaicManager.PaneCount + _imageManager.ImageCount) % _imageManager.ImageCount;
            
            _logger.LogTrace("New currentIndex after Previous: {CurrentIndex}", _currentIndex);
            _debugLogger.Log($"[NAV] New currentIndex after Previous: {_currentIndex}");
            
            _mosaicManager.ResetPaneIndex();
            _ = ShowImageAsync(_currentIndex);
            FlashSideRequested?.Invoke(false);
            
            if (!_pauseController.IsPaused)
                _slideshowController.Start();
        }

        public void OnSlideshowTick()
        {
            _isAutomaticNavigation = true; // Mark this as automatic navigation
            _currentIndex = (_currentIndex + _mosaicManager.PaneCount) % _imageManager.ImageCount;
            _mosaicManager.ResetPaneIndex();
            _ = ShowImageAsync(_currentIndex);
        }

        public async Task ShowImageAsync(int index)
        {
            _debugLogger.Log($"[SHOW] ShowImageAsync called with index: {index}, imageCount: {_imageManager.ImageCount}, isAutomatic: {_isAutomaticNavigation}");
            
            if (_imageManager.ImageCount > 0 && index >= 0 && index < _imageManager.ImageCount)
            {
                // Enable transition for automatic navigation BEFORE updating images
                if (_isAutomaticNavigation)
                {
                    _debugLogger.Log($"[ANIM] Enabling slide transition for automatic navigation");
                    SlideTransitionRequested?.Invoke();
                }
                
                // Get images to display (supports both lazy loading and legacy mode)
                _debugLogger.Log($"[SHOW] Calling GetImagesAsync(index={index}, paneCount={_mosaicManager.PaneCount})");
                var imagesToShow = await _imageManager.GetImagesAsync(index, _mosaicManager.PaneCount);
                
                _debugLogger.Log($"[SHOW] GetImagesAsync returned {imagesToShow.Count} images");
                
                if (imagesToShow.Count > 0)
                {
                    // Update the images - this will trigger the animation if enabled
                    ImagesDisplayRequested?.Invoke(imagesToShow);

                    // Request mosaic layout update for orientation detection
                    MosaicLayoutUpdateRequested?.Invoke();

                    var fileName = _imageManager.GetImageFileName(index);
                    var logMsg = $"Showing: {fileName}";
                    if (_mosaicManager.PaneCount > 1)
                        logMsg += $" (+{_mosaicManager.PaneCount - 1} more)";
                    _debugLogger.Log(logMsg);
                    
                    // Preload next images in background for smooth playback (after displaying current images)
                    _ = _imageManager.PreloadImagesAsync(index, _mosaicManager.PaneCount);
                }
                
                // Reset automatic navigation flag after processing
                _isAutomaticNavigation = false;
            }
        }

        public void ShowImage(int index)
        {
            _ = ShowImageAsync(index);
        }

        public void UpdateMosaicLayout(UniformGrid itemsPanel, double actualWidth, double actualHeight)
        {
            if (itemsPanel != null)
            {
                _mosaicManager.UpdateGridLayout(itemsPanel, actualWidth, actualHeight);
            }
        }
    }
}