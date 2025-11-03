using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ImageGallery.Services
{
    /// <summary>
    /// Handles display-related functionality including layout updates, visual effects, and mouse interactions.
    /// Single Responsibility: Manage UI display operations and visual feedback.
    /// </summary>
    public class DisplayService
    {
        private readonly ILogger<DisplayService> _logger;
        private readonly MosaicManager _mosaicManager;
        private readonly ZoomController _zoomController;
        private readonly NavigationService _navigationService;

        // Click detection state
        private Point _mouseDownPosition;
        private DateTime _mouseDownTime;
        private bool _isMouseDown = false;
        private const double CLICK_THRESHOLD_PIXELS = 5.0; // Maximum movement to still be considered a click
        private const int CLICK_THRESHOLD_MS = 500; // Maximum time to still be considered a click

        // Events for UI coordination
        public event Action<string>? LogMessageRequested;

        public DisplayService(
            ILogger<DisplayService> logger,
            MosaicManager mosaicManager,
            ZoomController zoomController,
            NavigationService navigationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mosaicManager = mosaicManager ?? throw new ArgumentNullException(nameof(mosaicManager));
            _zoomController = zoomController ?? throw new ArgumentNullException(nameof(zoomController));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        }

        /// <summary>
        /// Updates the mosaic layout for the given UniformGrid based on window dimensions.
        /// </summary>
        /// <param name="mosaicDisplay">The ItemsControl containing the UniformGrid</param>
        /// <param name="windowWidth">Current window width</param>
        /// <param name="windowHeight">Current window height</param>
        public void UpdateMosaicLayout(object mosaicDisplay, double windowWidth, double windowHeight)
        {
            var itemsPanel = FindVisualChild<UniformGrid>(mosaicDisplay as DependencyObject);
            if (itemsPanel != null)
            {
                _mosaicManager.UpdateGridLayout(itemsPanel, windowWidth, windowHeight);
                _logger.LogTrace("Updated mosaic layout: {PaneCount} panes, {Width}x{Height}", 
                    _mosaicManager.PaneCount, windowWidth, windowHeight);
            }
        }

        /// <summary>
        /// Creates a visual flash effect on the specified side of the screen.
        /// </summary>
        /// <param name="isRight">True for right side flash, false for left side</param>
        /// <param name="rightFlash">Right flash UI element</param>
        /// <param name="leftFlash">Left flash UI element</param>
        public async Task FlashSideAsync(bool isRight, UIElement rightFlash, UIElement leftFlash)
        {
            var flash = isRight ? rightFlash : leftFlash;
            if (flash == null) return;

            flash.Opacity = 0.3;

            for (var i = 3; i >= 0; i--)
            {
                flash.Opacity = i * 0.1;
                await Task.Delay(10);
            }
        }

        /// <summary>
        /// Handles mouse wheel events for zoom functionality.
        /// </summary>
        public void HandleMouseWheel(MouseWheelEventArgs e)
        {
            if (!_mosaicManager.IsMosaicMode)
            {
                _zoomController.HandleMouseWheel(e);
            }
        }

        /// <summary>
        /// Handles mouse left button down events for drag operations and click detection.
        /// </summary>
        public bool HandleMouseLeftButtonDown(MouseButtonEventArgs e, Window window, object mosaicDisplay)
        {
            // Record click state for single-click detection
            _mouseDownPosition = e.GetPosition(window);
            _mouseDownTime = DateTime.Now;
            _isMouseDown = true;

            if (!_mosaicManager.IsMosaicMode && _zoomController.IsZoomed)
            {
                _zoomController.StartDrag(_mouseDownPosition);
                if (mosaicDisplay is IInputElement element)
                {
                    element.CaptureMouse();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Handles mouse left button up events to end drag operations and detect single clicks.
        /// </summary>
        public void HandleMouseLeftButtonUp(object mosaicDisplay)
        {
            if (_isMouseDown)
            {
                // Check if this was a single click (not a drag)
                var currentTime = DateTime.Now;
                var timeDiff = (currentTime - _mouseDownTime).TotalMilliseconds;
                
                // Get current mouse position for movement calculation
                if (mosaicDisplay is IInputElement element)
                {
                    var currentPosition = Mouse.GetPosition(element);
                    var distance = Math.Sqrt(
                        Math.Pow(currentPosition.X - _mouseDownPosition.X, 2) + 
                        Math.Pow(currentPosition.Y - _mouseDownPosition.Y, 2));

                    // If it's a single click (minimal movement and quick), navigate to next image
                    // Only navigate if we're not in mosaic mode and not zoomed in (single image view)
                    if (distance <= CLICK_THRESHOLD_PIXELS && timeDiff <= CLICK_THRESHOLD_MS)
                    {
                        if (!_mosaicManager.IsMosaicMode && !_zoomController.IsZoomed)
                        {
                            _logger.LogDebug("Single click detected, navigating to next image. Distance: {Distance:F2}px, Time: {Time:F0}ms", 
                                distance, timeDiff);
                            _navigationService.NavigateNext();
                        }
                        else
                        {
                            _logger.LogTrace("Single click detected but navigation skipped - MosaicMode: {MosaicMode}, Zoomed: {Zoomed}", 
                                _mosaicManager.IsMosaicMode, _zoomController.IsZoomed);
                        }
                    }
                    else
                    {
                        _logger.LogTrace("Click not registered as single click. Distance: {Distance:F2}px, Time: {Time:F0}ms", 
                            distance, timeDiff);
                    }
                }

                _isMouseDown = false;
            }

            _zoomController.EndDrag();
            if (mosaicDisplay is IInputElement element2)
            {
                element2.ReleaseMouseCapture();
            }
        }

        /// <summary>
        /// Handles mouse move events for drag operations.
        /// </summary>
        public void HandleMouseMove(MouseEventArgs e, Window window)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _zoomController.UpdateDrag(e.GetPosition(window));
            }
        }

        /// <summary>
        /// Finds a visual child of the specified type in the visual tree.
        /// </summary>
        /// <typeparam name="T">Type of visual child to find</typeparam>
        /// <param name="parent">Parent dependency object to search in</param>
        /// <returns>First found child of type T, or null if not found</returns>
        public T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}