using System;
using System.Windows;
using System.Windows.Input;
using ImageGallery.Resources;
using Microsoft.Extensions.Logging;

namespace ImageGallery.Services;

/// <summary>
/// Handles keyboard input and delegates commands to appropriate services.
/// Single Responsibility: Process keyboard events and route commands.
/// </summary>
public class KeyboardCommandService
{
    private readonly ILogger<KeyboardCommandService> _logger;
    private readonly ImageManager _imageManager;
    private readonly ZoomController _zoomController;
    private readonly MosaicManager _mosaicManager;
    private readonly SlideshowController _slideshowController;
    private readonly PauseController _pauseController;
    private readonly DebugLogger _debugLogger;

    // Events for actions that require MainWindow coordination
    public event Action? NavigateNextRequested;
    public event Action? NavigatePreviousRequested;
    public event Action? ToggleFullscreenRequested;
    public event Action? SelectDirectoryRequested;

    public KeyboardCommandService(
        ILogger<KeyboardCommandService> logger,
        ImageManager imageManager,
        ZoomController zoomController,
        MosaicManager mosaicManager,
        SlideshowController slideshowController,
        PauseController pauseController,
        DebugLogger debugLogger)
    {
        _logger = logger;
        _imageManager = imageManager;
        _zoomController = zoomController;
        _mosaicManager = mosaicManager;
        _slideshowController = slideshowController;
        _pauseController = pauseController;
        _debugLogger = debugLogger;
    }

    /// <summary>
    /// Processes keyboard input and executes appropriate commands.
    /// </summary>
    /// <param name="e">Keyboard event arguments</param>
    /// <returns>True if the key was handled, false otherwise</returns>
    public bool HandleKeyDown(KeyEventArgs e)
    {
        _logger.LogTrace("Processing key: {Key}, Modifiers: {Modifiers}", e.Key, Keyboard.Modifiers);

        // Handle Ctrl+W or Ctrl+Q to close application
        if (HandleApplicationCloseCommands(e))
            return true;

        // Handle Space or Enter to pause/resume
        if (HandlePauseResumeCommands(e))
            return true;

        // Handle other key commands
        return HandleGeneralCommands(e);
    }

    private bool HandleApplicationCloseCommands(KeyEventArgs e)
    {
        if ((e.Key == Key.W || e.Key == Key.Q) && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (e.Key == Key.Q)
            {
                var result = MessageBox.Show(
                    "Do you want to delete all image files in this directory?",
                    "Delete Images",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _imageManager.DeleteAllImages();
                }
            }

            _debugLogger.Log("Application closing");
            _logger.LogInformation("Application closing via keyboard command: {Key}", e.Key);
            Application.Current.Shutdown();
            return true;
        }
        return false;
    }

    private bool HandlePauseResumeCommands(KeyEventArgs e)
    {
        if (e.Key == Key.Space || e.Key == Key.Enter)
        {
            // If we're paused and zoomed in, reset zoom when resuming
            if (_pauseController.IsPaused && _zoomController.IsZoomed)
            {
                _zoomController.ResetZoom();
                _logger.LogDebug("Reset zoom to 100% before resuming slideshow");
            }
            
            _pauseController.Toggle();
            _logger.LogDebug("Pause/Resume toggled via keyboard: {Key}", e.Key);
            return true;
        }
        return false;
    }

    private bool HandleGeneralCommands(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
            case Key.Right:
                NavigateNextRequested?.Invoke();
                _logger.LogTrace("Navigate next requested via {Key}", e.Key);
                return true;

            case Key.Down:
            case Key.Left:
                NavigatePreviousRequested?.Invoke();
                _logger.LogTrace("Navigate previous requested via {Key}", e.Key);
                return true;

            case Key.F:
                ToggleFullscreenRequested?.Invoke();
                _logger.LogDebug("Toggle fullscreen requested");
                return true;

            case Key.D:
                _debugLogger.Toggle();
                _logger.LogDebug("Debug console toggled");
                return true;

            case Key.OemComma: // < key
            case Key.OemPeriod: // > key
                return HandleSpeedCommands(e);

            case Key.OemMinus:
            case Key.OemPlus:
                return HandleZoomCommands(e);

            case Key.M:
                _mosaicManager.IncreasePanes();
                _logger.LogDebug("Mosaic panes increased to {PaneCount}", _mosaicManager.PaneCount);
                return true;

            case Key.N:
                _mosaicManager.DecreasePanes();
                _logger.LogDebug("Mosaic panes decreased to {PaneCount}", _mosaicManager.PaneCount);
                return true;

            case Key.I:
                SelectDirectoryRequested?.Invoke();
                _logger.LogDebug("Directory selection requested");
                return true;

            default:
                return false;
        }
    }

    private bool HandleSpeedCommands(KeyEventArgs e)
    {
        if (e.Key == Key.OemComma && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            _slideshowController.IncreaseSpeed();
            _slideshowController.Restart();
            if (_pauseController.IsPaused) _slideshowController.Stop();
            _logger.LogDebug("Slideshow speed increased to {Speed}s", _slideshowController.IntervalSeconds);
            return true;
        }
        else if (e.Key == Key.OemPeriod && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            _slideshowController.DecreaseSpeed();
            _slideshowController.Restart();
            if (_pauseController.IsPaused) _slideshowController.Stop();
            _logger.LogDebug("Slideshow speed decreased to {Speed}s", _slideshowController.IntervalSeconds);
            return true;
        }
        return false;
    }

    private bool HandleZoomCommands(KeyEventArgs e)
    {
        if (!_mosaicManager.IsMosaicMode)
        {
            if (e.Key == Key.OemMinus)
            {
                _zoomController.ZoomOut();
                _logger.LogTrace("Zoom out to {ZoomPercent}%", _zoomController.ZoomPercent);
            }
            else
            {
                _zoomController.ZoomIn();
                _logger.LogTrace("Zoom in to {ZoomPercent}%", _zoomController.ZoomPercent);
            }
            return true;
        }
        return false;
    }
}