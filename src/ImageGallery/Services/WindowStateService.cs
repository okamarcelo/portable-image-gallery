using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;

namespace ImageGallery.Services;

/// <summary>
/// Manages window state, fullscreen mode, mouse interactions, and cursor behavior.
/// Single Responsibility: Handle all window-related state and behavior.
/// </summary>
public class WindowStateService
{
    private readonly ILogger<WindowStateService> _logger;
    private readonly ZoomController _zoomController;
    
    private Window? _window;
    private bool _isFullscreen = false;
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;

    public bool IsFullscreen => _isFullscreen;
    
    public event Action<string>? LogMessage;

    public WindowStateService(ILogger<WindowStateService> logger, ZoomController zoomController)
    {
        _logger = logger;
        _zoomController = zoomController;
    }

    /// <summary>
    /// Initialize the service with the window reference.
    /// </summary>
    /// <param name="window">The main window to manage</param>
    public void Initialize(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _logger.LogDebug("WindowStateService initialized with window");
    }

    /// <summary>
    /// Toggle between fullscreen and windowed mode.
    /// </summary>
    public void ToggleFullscreen()
    {
        if (_window == null)
        {
            _logger.LogWarning("Cannot toggle fullscreen: Window not initialized");
            return;
        }

        if (!_isFullscreen)
        {
            _previousWindowState = _window.WindowState;
            _previousWindowStyle = _window.WindowStyle;
            _window.WindowStyle = WindowStyle.None;
            _window.WindowState = WindowState.Maximized;
            _isFullscreen = true;
            _logger.LogDebug("Entered fullscreen mode");
            LogMessage?.Invoke("Entered fullscreen mode");
        }
        else
        {
            _window.WindowStyle = _previousWindowStyle;
            _window.WindowState = _previousWindowState;
            _isFullscreen = false;
            _logger.LogDebug("Exited fullscreen mode");
            LogMessage?.Invoke("Exited fullscreen mode");
        }
    }

    /// <summary>
    /// Handle mouse left button down for window dragging.
    /// </summary>
    /// <param name="position">Mouse position relative to window</param>
    /// <returns>True if drag was initiated, false otherwise</returns>
    public bool HandleMouseLeftButtonDown(Point position)
    {
        if (_window == null) return false;

        // Allow dragging window when not in fullscreen, not zoomed, and not near edges (for resizing)
        if (!_isFullscreen && !_zoomController.IsZoomed && !IsNearEdge(position))
        {
            try
            {
                _window.DragMove();
                _logger.LogTrace("Window drag initiated");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Window drag failed (normal when not clicking on window chrome)");
                // Ignore exceptions when dragging - this is normal behavior
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Handle mouse movement for cursor updates.
    /// </summary>
    /// <param name="position">Mouse position relative to window</param>
    public void HandleMouseMove(Point position)
    {
        if (_window == null) return;

        // Update cursor based on position for resize indication
        if (_isFullscreen || _zoomController.IsZoomed)
            return;

        UpdateCursorForResize(position);
    }

    /// <summary>
    /// Check if a point is near the window edge for resize operations.
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <returns>True if near edge, false otherwise</returns>
    public bool IsNearEdge(Point position)
    {
        if (_window == null) return false;

        const double resizeBorder = 5;
        
        var nearLeft = position.X <= resizeBorder;
        var nearRight = position.X >= _window.ActualWidth - resizeBorder;
        var nearTop = position.Y <= resizeBorder;
        var nearBottom = position.Y >= _window.ActualHeight - resizeBorder;

        return nearLeft || nearRight || nearTop || nearBottom;
    }

    /// <summary>
    /// Update cursor based on mouse position for resize indication.
    /// </summary>
    /// <param name="position">Mouse position relative to window</param>
    private void UpdateCursorForResize(Point position)
    {
        if (_window == null) return;

        const double resizeBorder = 5;
        
        var nearLeft = position.X <= resizeBorder;
        var nearRight = position.X >= _window.ActualWidth - resizeBorder;
        var nearTop = position.Y <= resizeBorder;
        var nearBottom = position.Y >= _window.ActualHeight - resizeBorder;

        // Set cursor based on position
        if ((nearLeft && nearTop) || (nearRight && nearBottom))
        {
            _window.Cursor = Cursors.SizeNWSE;
        }
        else if ((nearLeft && nearBottom) || (nearRight && nearTop))
        {
            _window.Cursor = Cursors.SizeNESW;
        }
        else if (nearLeft || nearRight)
        {
            _window.Cursor = Cursors.SizeWE;
        }
        else if (nearTop || nearBottom)
        {
            _window.Cursor = Cursors.SizeNS;
        }
        else
        {
            _window.Cursor = Cursors.Arrow;
        }
    }
}