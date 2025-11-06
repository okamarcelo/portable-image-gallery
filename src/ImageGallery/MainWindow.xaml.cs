using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private readonly WindowStateService _windowStateService;
        private readonly ImageLoaderService _imageLoaderService;
        private readonly NavigationService _navigationService;
        private readonly DisplayService _displayService;
        private readonly TransitionAnimationService _transitionAnimationService;

        // UI state
        // _currentIndex is now managed by NavigationService
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
        KeyboardCommandService keyboardCommandService,
        WindowStateService windowStateService,
        ImageLoaderService imageLoaderService,
        NavigationService navigationService,
        DisplayService displayService,
        TransitionAnimationService transitionAnimationService)
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
            this._windowStateService = windowStateService;
            this._imageLoaderService = imageLoaderService;
            this._navigationService = navigationService;
            this._displayService = displayService;
            this._transitionAnimationService = transitionAnimationService;
            
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
                _navigationService.ShowImage(_navigationService.CurrentIndex);
                if (_zoomController.IsZoomed)
                {
                    _zoomController.ResetZoom();
                }
            };
            _mosaicManager.LogMessage += msg => _debugLogger.Log(msg);

            // SlideshowController events
            _slideshowController.Tick += _navigationService.OnSlideshowTick;
            _slideshowController.IntervalChanged += interval => _indicatorManager.ShowSpeed(interval);
            _slideshowController.LogMessage += msg => _debugLogger.Log(msg);

            // PauseController events
            _pauseController.Paused += () => _slideshowController.Stop();
            _pauseController.Resumed += () => _slideshowController.Start();
            _pauseController.LogMessage += msg => _debugLogger.Log(msg);

            // KeyboardCommandService events
            _keyboardCommandService.NavigateNextRequested += _navigationService.NavigateNext;
            _keyboardCommandService.NavigatePreviousRequested += _navigationService.NavigatePrevious;
            _keyboardCommandService.ToggleFullscreenRequested += _windowStateService.ToggleFullscreen;
            _keyboardCommandService.SelectDirectoryRequested += () => _ = _imageLoaderService.SelectRootDirectoryAsync();

            // WindowStateService events
            _windowStateService.LogMessage += msg => _debugLogger.Log(msg);

            // ImageLoaderService events
            _imageLoaderService.LoadingOverlayVisibilityChanged += visibility => 
                Dispatcher.Invoke(() => LoadingOverlay.Visibility = visibility);
            _imageLoaderService.LoadingProgressStackVisibilityChanged += visibility => 
                Dispatcher.Invoke(() => LoadingProgressStack.Visibility = visibility);
            _imageLoaderService.ImportProgressStackVisibilityChanged += visibility => 
                Dispatcher.Invoke(() => ImportProgressStack.Visibility = visibility);
            _imageLoaderService.LoadingTextChanged += text => 
                Dispatcher.Invoke(() => LoadingText.Text = text);
            _imageLoaderService.ShuffleImagesRequested += ShuffleImages;
            _imageLoaderService.ShowImageRequested += _navigationService.ShowImage;
            _imageLoaderService.SlideshowStartRequested += () => _slideshowController.Start();

            // NavigationService events
            _navigationService.ImagesDisplayRequested += images => 
                Dispatcher.Invoke(() => MosaicDisplay.ItemsSource = images);
            _navigationService.MosaicLayoutUpdateRequested += () => 
                _displayService.UpdateMosaicLayout(MosaicDisplay.CurrentItemsControl, ActualWidth, ActualHeight);
            _navigationService.FlashSideRequested += isRight => 
                _ = _displayService.FlashSideAsync(isRight, RightFlash, LeftFlash);
            _navigationService.SlideTransitionRequested += () =>
                _transitionAnimationService.EnableTransitionOnce(MosaicDisplay);

            // DisplayService events
            _displayService.LogMessageRequested += msg => _debugLogger.Log(msg);
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
            
            // Initialize zoom controller with transforms from the sliding control
            if (MosaicDisplay.CurrentScaleTransform != null && MosaicDisplay.CurrentTranslateTransform != null)
            {
                _zoomController.Initialize(MosaicDisplay.CurrentScaleTransform, MosaicDisplay.CurrentTranslateTransform);
            }
            
            _windowStateService.Initialize(this);

            _debugLogger.Log(Strings.Status_ApplicationStarted);
            _logger.LogDebug(Strings.SLog_UIComponentsInitialized);

            // Handle CLI arguments if provided
            if (_cliArgs != null && _cliArgs.IsCliMode)
            {
                await _imageLoaderService.LoadFromCliArgumentsAsync(_cliArgs);
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
                _navigationService.ShowImage(0);
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
                await _imageLoaderService.SelectRootDirectoryAsync();
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

        
        private void Window_MouseLeftButtonDown_Border(object sender, MouseButtonEventArgs e)
        {
            _windowStateService.HandleMouseLeftButtonDown(e.GetPosition(this));
        }

        private void Window_MouseLeftButtonUp_Border(object sender, MouseButtonEventArgs e)
        {
            _windowStateService.HandleMouseLeftButtonUp();
        }

        private void Window_MouseRightButtonDown_Border(object sender, MouseButtonEventArgs e)
        {
            _displayService.HandleMouseRightButtonDown(e, this);
        }

        private void Window_MouseRightButtonUp_Border(object sender, MouseButtonEventArgs e)
        {
            var mosaicDisplay = this.FindName("MosaicDisplay");
            if (mosaicDisplay != null)
            {
                _displayService.HandleMouseRightButtonUp(mosaicDisplay);
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            _windowStateService.HandleMouseMove(e.GetPosition(this));
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
                var mosaicDisplay = this.FindName("MosaicDisplay");
                if (mosaicDisplay != null)
                {
                    _displayService.UpdateMosaicLayout(mosaicDisplay, ActualWidth, ActualHeight);
                }
            }
        }

        // OnSlideshowTick is now handled by NavigationService

        // ShowImage and ShowImageAsync are now handled by NavigationService

        // UpdateMosaicLayout is now handled by DisplayService

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

        // NavigateNext and NavigatePrevious are now handled by NavigationService

        // FlashSide is now handled by DisplayService







        // Zoom and Pan event handlers - delegated to DisplayService
        private void MosaicDisplay_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _displayService.HandleMouseWheel(e);
        }

        private void MosaicDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _displayService.HandleMouseLeftButtonDown(e, this, sender);
        }

        private void MosaicDisplay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _displayService.HandleMouseLeftButtonUp(sender);
        }

        private void MosaicDisplay_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _displayService.HandleMouseRightButtonDown(e, this);
        }

        private void MosaicDisplay_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _displayService.HandleMouseRightButtonUp(sender);
        }

        private void MosaicDisplay_MouseMove(object sender, MouseEventArgs e)
        {
            _displayService.HandleMouseMove(e, this);
        }

        // FindVisualChild is now handled by DisplayService
}

