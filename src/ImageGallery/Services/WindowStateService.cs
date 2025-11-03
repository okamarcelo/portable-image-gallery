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

    // Drag state tracking
    private bool _isDragReady = false;
    private Point _dragStartPosition;
    private DateTime _mouseDownTime;
    private const double DRAG_THRESHOLD_PIXELS = 5.0;
    private const int DRAG_DELAY_MS = 100;

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
    /// Handle mouse left button down for potential window dragging.
    /// Only prepares for drag, actual drag starts on mouse move.
    /// </summary>
    /// <param name="position">Mouse position relative to window</param>
    /// <returns>True if ready for drag, false otherwise</returns>
    public bool HandleMouseLeftButtonDown(Point position)
    {
        if (_window == null) return false;

        // Prepare for dragging window when not in fullscreen, not zoomed, and not near edges (for resizing)
        if (!_isFullscreen && !_zoomController.IsZoomed && !IsNearEdge(position))
        {
            _isDragReady = true;
            _dragStartPosition = position;
            _mouseDownTime = DateTime.Now;
            _logger.LogTrace("Window drag prepared at position {X:F0},{Y:F0}", position.X, position.Y);
            return true;
        }
        
        _isDragReady = false;
        return false;
    }

    /// <summary>
    /// Handle mouse left button up to reset drag state.
    /// </summary>
    public void HandleMouseLeftButtonUp()
    {
        _isDragReady = false;
        _logger.LogTrace("Window drag state reset");
    }

    /// <summary>
    /// Handle mouse movement for cursor updates and delayed drag initiation.
    /// </summary>
    /// <param name="position">Mouse position relative to window</param>
    public void HandleMouseMove(Point position)
    {
        if (_window == null) return;

        // Check if we should initiate drag
        if (_isDragReady && Mouse.LeftButton == MouseButtonState.Pressed)
        {
            var distance = Math.Sqrt(
                Math.Pow(position.X - _dragStartPosition.X, 2) + 
                Math.Pow(position.Y - _dragStartPosition.Y, 2));
            var timePassed = (DateTime.Now - _mouseDownTime).TotalMilliseconds;

            // Only start drag if mouse moved enough OR enough time passed (to prevent accidental clicks)
            if (distance >= DRAG_THRESHOLD_PIXELS || timePassed >= DRAG_DELAY_MS)
            {
                try
                {
                    _isDragReady = false; // Reset before drag to prevent multiple calls
                    _window.DragMove();
                    _logger.LogTrace("Window drag initiated after move. Distance: {Distance:F2}px, Time: {Time:F0}ms", 
                        distance, timePassed);
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Window drag failed during mouse move");
                }
                return;
            }
        }

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