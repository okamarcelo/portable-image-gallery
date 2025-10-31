using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageGallery.Services;
using ImageGallery.Resources;
using Microsoft.Extensions.Logging;

namespace ImageGallery;

/// <summary>
/// Main window - coordinates UI and delegates to specialized services.
/// </summary>
public partial class MainWindow : Window
    {
        // Services (Dependency Injection pattern)
        private readonly ILogger<MainWindow> _logger;
        private readonly ImageManager _imageManager;
        private readonly ZoomController _zoomController;
        private readonly MosaicManager _mosaicManager;
        private readonly SlideshowController _slideshowController;
        private readonly PauseController _pauseController;
        private readonly DebugLogger _debugLogger;
        private readonly IndicatorManager _indicatorManager;
        private readonly KeyboardCommandService _keyboardCommandService;

        // UI state
        private int _currentIndex = 0;
        private bool _isFullscreen = false;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;
        private CommandLineArguments? _cliArgs;

    public MainWindow(
        ILogger<MainWindow> logger,
        ImageManager imageManager,
        ZoomController zoomController,
        MosaicManager mosaicManager,
        SlideshowController slideshowController,
        PauseController pauseController,
        DebugLogger debugLogger,
        IndicatorManager indicatorManager,
        KeyboardCommandService keyboardCommandService)
    {
        try
        {
            _logger = logger;
            this._imageManager = imageManager;
            this._zoomController = zoomController;
            this._mosaicManager = mosaicManager;
            this._slideshowController = slideshowController;
            this._pauseController = pauseController;
            this._debugLogger = debugLogger;
            this._indicatorManager = indicatorManager;
            this._keyboardCommandService = keyboardCommandService;
            
            _logger.LogInformation(Strings.SLog_MainWindowInitializing);
            InitializeComponent();

            // Wire up event handlers
            _logger.LogDebug(Strings.SLog_SettingUpEventHandlers);
            SetupEventHandlers();
            
            // Setup window size change handler for orientation-aware layout
            this.SizeChanged += MainWindow_SizeChanged;
            
            _logger.LogInformation(Strings.SLog_MainWindowInitializedSuccessfully);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, Strings.SLog_FailedToInitializeMainWindow);
            MessageBox.Show(string.Format(Strings.Error_InitializationMessage, ex.Message), 
                Strings.Error_InitializationTitle, 
                MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
    
    public void SetCommandLineArguments(CommandLineArguments commandLineArgs)
    {
        _cliArgs = commandLineArgs;
    }
    
    private void SetupEventHandlers()
        {
            // ImageManager events
            _imageManager.LoadProgressChanged += (current, total) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LoadingProgressBar.Value = current;
                    LoadingProgressBar.Maximum = total;
                    LoadingDetailsText.Text = string.Format(Strings.Progress_CurrentTotal, current, total);
                });
            };

            _imageManager.ImportProgressChanged += (current, total, errors) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ImportProgressBar.Value = current;
                    ImportProgressBar.Maximum = total;
                    ImportDetailsText.Text = string.Format(Strings.Progress_CurrentTotal, current, total) + 
                        (errors > 0 ? string.Format(Strings.Progress_CurrentTotalErrors, current, total, errors) : "");
                });
            };

            _imageManager.LogMessage += msg => _debugLogger.Log(msg);

            // ZoomController events
            _zoomController.ZoomChanged += level => _indicatorManager.ShowZoom(_zoomController.ZoomPercent);
            _zoomController.ZoomedIn += () => _pauseController.Pause();
            _zoomController.ZoomedOut += () => _pauseController.Resume();
            _zoomController.LogMessage += msg => _debugLogger.Log(msg);

            // MosaicManager events
            _mosaicManager.PaneCountChanged += paneCount =>
            {
                ShowImage(_currentIndex);
                if (_zoomController.IsZoomed)
                {
                    _zoomController.ResetZoom();
                }
            };
            _mosaicManager.LogMessage += msg => _debugLogger.Log(msg);

            // SlideshowController events
            _slideshowController.Tick += OnSlideshowTick;
            _slideshowController.IntervalChanged += interval => _indicatorManager.ShowSpeed(interval);
            _slideshowController.LogMessage += msg => _debugLogger.Log(msg);

            // PauseController events
            _pauseController.Paused += () => _slideshowController.Stop();
            _pauseController.Resumed += () => _slideshowController.Start();
            _pauseController.LogMessage += msg => _debugLogger.Log(msg);

            // KeyboardCommandService events
            _keyboardCommandService.NavigateNextRequested += NavigateNext;
            _keyboardCommandService.NavigatePreviousRequested += NavigatePrevious;
            _keyboardCommandService.ToggleFullscreenRequested += ToggleFullscreen;
            _keyboardCommandService.SelectDirectoryRequested += () => _ = SelectRootDirectoryAsync();
        }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation(Strings.SLog_WindowLoaded);
            
            // Initialize UI references
            _debugLogger.Initialize(DebugConsole, LogTextBlock);
            _pauseController.Initialize(PausePlayIcon, PauseBar1, PauseBar2, PlayTriangle);
            _indicatorManager.Initialize(SpeedIndicator, SpeedText, ZoomIndicator, ZoomText);
            _zoomController.Initialize(MosaicScaleTransform, MosaicTranslateTransform);

            _debugLogger.Log(Strings.Status_ApplicationStarted);
            _logger.LogDebug(Strings.SLog_UIComponentsInitialized);

            // Handle CLI arguments if provided
            if (_cliArgs != null && _cliArgs.IsCliMode)
            {
                await LoadFromCliArgumentsAsync();
                return;
            }

            // Normal startup - load images from default location
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingProgressStack.Visibility = Visibility.Visible;

            _logger.LogInformation(Strings.SLog_StartingImageLoading);
            await _imageManager.LoadImagesAsync();
            _logger.LogInformation(string.Format(Strings.SLog_ImageLoadingCompleted, _imageManager.ImageCount));

            if (_imageManager.ImageCount > 0)
            {
                // Shuffle images
                ShuffleImages();
                ShowImage(0);
                _slideshowController.Start();
                LoadingOverlay.Visibility = Visibility.Collapsed;
                _logger.LogInformation(Strings.SLog_SlideshowStarted);
            }
            else
            {
                var patternText = string.IsNullOrWhiteSpace(_imageManager.FolderPattern)
                    ? Strings.Pattern_AllSubdirectories
                    : string.Format(Strings.Pattern_SpecificFolders, _imageManager.FolderPattern);
                LoadingText.Text = string.Format(Strings.Error_NoImages, patternText);
                _logger.LogWarning(Strings.SLog_NoImagesFoundInDirectory);
                
                // Automatically show folder selection dialog
                LoadingProgressStack.Visibility = Visibility.Collapsed;
                await SelectRootDirectoryAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, Strings.SLog_ErrorDuringWindowLoad);
            MessageBox.Show(string.Format(Strings.Error_LoadMessage, ex.Message), 
                Strings.Error_LoadTitle, 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async System.Threading.Tasks.Task LoadFromCliArgumentsAsync()
    {
        try
        {
            if (_cliArgs == null || !_cliArgs.IsCliMode)
                return;

            _logger.LogInformation(Strings.SLog_LoadingFromCLI);

            // Set folder pattern (empty string means all subdirectories)
            _imageManager.FolderPattern = _cliArgs.FolderPattern ?? "";
            
            // Set pane count if specified
            if (_cliArgs.PaneCount.HasValue)
            {
                // Find the closest valid mosaic size
                var targetPanes = _cliArgs.PaneCount.Value;
                int[] validSizes = { 1, 2, 4, 9, 16 };
                
                // Find the exact match or closest valid size
                var closestSize = validSizes[0];
                foreach (var size in validSizes)
                {
                    if (size == targetPanes)
                    {
                        closestSize = size;
                        break;
                    }
                    else if (size < targetPanes)
                    {
                        closestSize = size;
                    }
                }

                // Set the pane count by calling IncreasePanes/DecreasePanes
                while (_mosaicManager.PaneCount != closestSize)
                {
                    if (_mosaicManager.PaneCount < closestSize)
                        _mosaicManager.IncreasePanes();
                    else
                        _mosaicManager.DecreasePanes();
                }

                _logger.LogInformation(string.Format(Strings.SLog_SetPaneCount, _mosaicManager.PaneCount));
            }

            // Set fullscreen mode if specified
            if (_cliArgs.Fullscreen)
            {
                // Note: We need to set fullscreen after the window is fully loaded
                // So we'll do it after loading images
                _logger.LogInformation(Strings.SLog_FullscreenModeWillBeActivated);
            }

            // Load images from specified directory
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingProgressStack.Visibility = Visibility.Visible;
            
            var patternText = string.IsNullOrWhiteSpace(_imageManager.FolderPattern)
                ? Strings.Pattern_AllSubdirectories
                : string.Format(Strings.Pattern_SpecificFolders, _imageManager.FolderPattern);
            LoadingText.Text = string.Format(Strings.Loading_ImagesFrom, patternText);

            await _imageManager.LoadImagesFromDirectoryAsync(_cliArgs.RootDirectory!);
            
            if (_imageManager.ImageCount > 0)
            {
                ShuffleImages();
                ShowImage(0);
                _slideshowController.Start();
                LoadingOverlay.Visibility = Visibility.Collapsed;
                
                // Activate fullscreen mode if requested via CLI
                if (_cliArgs.Fullscreen)
                {
                    ToggleFullscreen();
                    _logger.LogInformation(Strings.SLog_ActivatedFullscreenMode);
                }
                
                _logger.LogInformation(string.Format(Strings.SLog_CLIModeLoadedImages, _imageManager.ImageCount));
            }
            else
            {
                LoadingText.Text = string.Format(Strings.Error_NoImagesInPattern, patternText);
                LoadingProgressStack.Visibility = Visibility.Collapsed;
                _logger.LogWarning(Strings.SLog_CLIModeNoImages);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, Strings.SLog_ErrorLoadingFromCLI);
            LoadingOverlay.Visibility = Visibility.Collapsed;
            MessageBox.Show(string.Format(Strings.Error_LoadImagesMessage, ex.Message), 
                Strings.Error_LoadTitle, 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }        
        private void Window_MouseLeftButtonDown_Border(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging window when not in fullscreen, not zoomed, and not near edges (for resizing)
            if (!_isFullscreen && !_zoomController.IsZoomed && !IsNearEdge(e.GetPosition(this)))
            {
                try
                {
                    this.DragMove();
                }
                catch
                {
                    // Ignore exceptions when dragging
                }
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            // Update cursor based on position for resize indication
            if (_isFullscreen || _zoomController.IsZoomed)
                return;

            var position = e.GetPosition(this);
            UpdateCursorForResize(position);
        }

        private bool IsNearEdge(Point position)
        {
            const double resizeBorder = 5;
            
            var nearLeft = position.X <= resizeBorder;
            var nearRight = position.X >= ActualWidth - resizeBorder;
            var nearTop = position.Y <= resizeBorder;
            var nearBottom = position.Y >= ActualHeight - resizeBorder;

            return nearLeft || nearRight || nearTop || nearBottom;
        }

        private void UpdateCursorForResize(Point position)
        {
            const double resizeBorder = 5;
            
            var nearLeft = position.X <= resizeBorder;
            var nearRight = position.X >= ActualWidth - resizeBorder;
            var nearTop = position.Y <= resizeBorder;
            var nearBottom = position.Y >= ActualHeight - resizeBorder;

            // Set cursor based on position
            if ((nearLeft && nearTop) || (nearRight && nearBottom))
            {
                Cursor = Cursors.SizeNWSE;
            }
            else if ((nearLeft && nearBottom) || (nearRight && nearTop))
            {
                Cursor = Cursors.SizeNESW;
            }
            else if (nearLeft || nearRight)
            {
                Cursor = Cursors.SizeWE;
            }
            else if (nearTop || nearBottom)
            {
                Cursor = Cursors.SizeNS;
            }
            else
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // This method is kept for backward compatibility but not used anymore
            // The Window_MouseLeftButtonDown_Border is now used instead
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Update mosaic layout when window size changes (for orientation-aware 2-pane layout)
            if (_mosaicManager.PaneCount == 2 && _imageManager.ImageCount > 0)
            {
                UpdateMosaicLayout();
            }
        }

        private void OnSlideshowTick()
        {
            _currentIndex = (_currentIndex + _mosaicManager.PaneCount) % _imageManager.ImageCount;
            _mosaicManager.ResetPaneIndex();
            _ = ShowImageAsync(_currentIndex);
        }

        private async Task ShowImageAsync(int index)
        {
            _debugLogger.Log($"[SHOW] ShowImageAsync called with index: {index}, imageCount: {_imageManager.ImageCount}");
            
            if (_imageManager.ImageCount > 0 && index >= 0 && index < _imageManager.ImageCount)
            {
                // Get images to display (supports both lazy loading and legacy mode)
                _debugLogger.Log($"[SHOW] Calling GetImagesAsync(index={index}, paneCount={_mosaicManager.PaneCount})");
                var imagesToShow = await _imageManager.GetImagesAsync(index, _mosaicManager.PaneCount);
                
                _debugLogger.Log($"[SHOW] GetImagesAsync returned {imagesToShow.Count} images");
                
                if (imagesToShow.Count > 0)
                {
                    MosaicDisplay.ItemsSource = imagesToShow;

                    // Update the grid layout with window dimensions for orientation detection
                    UpdateMosaicLayout();

                    var fileName = _imageManager.GetImageFileName(index);
                    var logMsg = $"Showing: {fileName}";
                    if (_mosaicManager.PaneCount > 1)
                        logMsg += $" (+{_mosaicManager.PaneCount - 1} more)";
                    _debugLogger.Log(logMsg);
                    
                    // Preload next images in background for smooth playback (after displaying current images)
                    _ = _imageManager.PreloadImagesAsync(index, _mosaicManager.PaneCount);
                }
            }
        }

        private void ShowImage(int index)
        {
            _ = ShowImageAsync(index);
        }

        private void UpdateMosaicLayout()
        {
            var itemsPanel = FindVisualChild<UniformGrid>(MosaicDisplay);
            if (itemsPanel != null)
            {
                _mosaicManager.UpdateGridLayout(itemsPanel, ActualWidth, ActualHeight);
            }
        }

        private void ShuffleImages()
        {
            _imageManager.Shuffle();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Delegate keyboard handling to the KeyboardCommandService
            var handled = _keyboardCommandService.HandleKeyDown(e);
            e.Handled = handled;
        }

        private void NavigateNext()
        {
            _logger.LogTrace("Navigate Next: currentIndex={CurrentIndex}, imageCount={ImageCount}, paneCount={PaneCount}", 
                _currentIndex, _imageManager.ImageCount, _mosaicManager.PaneCount);
            _debugLogger.Log($"[NAV] Next pressed - currentIndex: {_currentIndex}, imageCount: {_imageManager.ImageCount}, paneCount: {_mosaicManager.PaneCount}");
            _slideshowController.Stop();
            _currentIndex = (_currentIndex + _mosaicManager.PaneCount) % _imageManager.ImageCount;
            _logger.LogTrace("New currentIndex after Next: {CurrentIndex}", _currentIndex);
            _debugLogger.Log($"[NAV] New currentIndex after Next: {_currentIndex}");
            _mosaicManager.ResetPaneIndex();
            ShowImage(_currentIndex);
            FlashSide(true);
            if (!_pauseController.IsPaused)
                _slideshowController.Start();
        }

        private void NavigatePrevious()
        {
            _logger.LogTrace("Navigate Previous: currentIndex={CurrentIndex}, imageCount={ImageCount}, paneCount={PaneCount}", 
                _currentIndex, _imageManager.ImageCount, _mosaicManager.PaneCount);
            _debugLogger.Log($"[NAV] Previous pressed - currentIndex: {_currentIndex}, imageCount: {_imageManager.ImageCount}, paneCount: {_mosaicManager.PaneCount}");
            _slideshowController.Stop();
            _currentIndex = (_currentIndex - _mosaicManager.PaneCount + _imageManager.ImageCount) % _imageManager.ImageCount;
            _logger.LogTrace("New currentIndex after Previous: {CurrentIndex}", _currentIndex);
            _debugLogger.Log($"[NAV] New currentIndex after Previous: {_currentIndex}");
            _mosaicManager.ResetPaneIndex();
            ShowImage(_currentIndex);
            FlashSide(false);
            if (!_pauseController.IsPaused)
                _slideshowController.Start();
        }

        private async void FlashSide(bool isRight)
        {
            var flash = isRight ? RightFlash : LeftFlash;
            flash.Opacity = 0.3;

            for (var i = 3; i >= 0; i--)
            {
                flash.Opacity = i * 0.1;
                await System.Threading.Tasks.Task.Delay(10);
            }
        }

        private async System.Threading.Tasks.Task ImportImagesAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingText.Text = "Importing and loading images...";
            ImportProgressStack.Visibility = Visibility.Visible;
            LoadingProgressStack.Visibility = Visibility.Visible;

            var imported = await _imageManager.ImportImagesAsync();

            if (imported > 0)
            {
                ImportProgressStack.Visibility = Visibility.Collapsed;
                await _imageManager.LoadImagesAsync();
                
                if (_imageManager.ImageCount > 0)
                {
                    ShuffleImages();
                    ShowImage(0);
                    _slideshowController.Start();
                }
            }

            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        private async System.Threading.Tasks.Task SelectRootDirectoryAsync()
        {
            try
            {
                _logger.LogInformation("Prompting for folder pattern");
                
                // First, ask for the folder pattern
                var patternDialog = new InputDialog(
                    "Enter the folder name pattern to search for:",
                    "Folder Pattern",
                    _imageManager.FolderPattern);

                if (patternDialog.ShowDialog() != true)
                {
                    _logger.LogInformation("User cancelled pattern input");
                    if (_imageManager.ImageCount == 0)
                    {
                        LoadingText.Text = "No pattern specified. Press I to select a directory.";
                        LoadingProgressStack.Visibility = Visibility.Collapsed;
                    }
                    return;
                }

                _imageManager.FolderPattern = patternDialog.ResponseText;
                
                var patternLogText = string.IsNullOrWhiteSpace(_imageManager.FolderPattern)
                    ? "all subdirectories"
                    : _imageManager.FolderPattern;
                _logger.LogInformation($"User set folder pattern to: {patternLogText}");
                
                _logger.LogInformation("Opening folder selection dialog");
                
                var patternText = string.IsNullOrWhiteSpace(_imageManager.FolderPattern)
                    ? "all subdirectories"
                    : $"'{_imageManager.FolderPattern}' folders";
                
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = $"Select root directory to search {patternText}",
                    ShowNewFolderButton = false,
                    UseDescriptionForTitle = true
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var selectedPath = dialog.SelectedPath;
                    _logger.LogInformation($"User selected directory: {selectedPath}");
                    
                    LoadingOverlay.Visibility = Visibility.Visible;
                    LoadingText.Text = $"Searching for images in {patternText}...";
                    LoadingProgressStack.Visibility = Visibility.Visible;

                    await _imageManager.LoadImagesFromDirectoryAsync(selectedPath);
                    
                    if (_imageManager.ImageCount > 0)
                    {
                        ShuffleImages();
                        ShowImage(0);
                        _slideshowController.Start();
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                        _logger.LogInformation($"Loaded {_imageManager.ImageCount} images from selected directory");
                    }
                    else
                    {
                        LoadingText.Text = $"No images found in {patternText}. Press I to try another directory.";
                        LoadingProgressStack.Visibility = Visibility.Collapsed;
                        _logger.LogWarning("No images found in selected directory");
                    }
                }
                else
                {
                    _logger.LogInformation("User cancelled folder selection");
                    if (_imageManager.ImageCount == 0)
                    {
                        LoadingText.Text = "No directory selected. Press I to select a directory.";
                        LoadingProgressStack.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting directory");
                MessageBox.Show($"Error selecting directory:\n{ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                _previousWindowState = WindowState;
                _previousWindowStyle = WindowStyle;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                _isFullscreen = true;
            }
            else
            {
                WindowStyle = _previousWindowStyle;
                WindowState = _previousWindowState;
                _isFullscreen = false;
            }
        }

        // Zoom and Pan event handlers
        private void MosaicDisplay_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_mosaicManager.IsMosaicMode)
            {
                _zoomController.HandleMouseWheel(e);
            }
        }

        private void MosaicDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_mosaicManager.IsMosaicMode && _zoomController.IsZoomed)
            {
                _zoomController.StartDrag(e.GetPosition(this));
                MosaicDisplay.CaptureMouse();
            }
        }

        private void MosaicDisplay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _zoomController.EndDrag();
            MosaicDisplay.ReleaseMouseCapture();
        }

        private void MosaicDisplay_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _zoomController.UpdateDrag(e.GetPosition(this));
            }
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
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

