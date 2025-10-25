using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageGallery.Services;

namespace ImageGallery;

/// <summary>
/// Main window - coordinates UI and delegates to specialized services.
/// </summary>
public partial class MainWindow : Window
    {
        // Services (Dependency Injection pattern)
        private readonly ImageManager imageManager;
        private readonly ZoomController zoomController;
        private readonly MosaicManager mosaicManager;
        private readonly SlideshowController slideshowController;
        private readonly PauseController pauseController;
        private readonly DebugLogger debugLogger;
        private readonly IndicatorManager indicatorManager;

        // UI state
        private int currentIndex = 0;
        private bool isFullscreen = false;
        private WindowState previousWindowState;
        private WindowStyle previousWindowStyle;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            imageManager = new ImageManager();
            zoomController = new ZoomController();
            mosaicManager = new MosaicManager();
            slideshowController = new SlideshowController();
            pauseController = new PauseController();
            debugLogger = new DebugLogger();
            indicatorManager = new IndicatorManager();

            // Wire up event handlers
            SetupEventHandlers();

            // Setup mouse handler for window dragging
            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
        }

        private void SetupEventHandlers()
        {
            // ImageManager events
            imageManager.LoadProgressChanged += (current, total) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LoadingProgressBar.Value = current;
                    LoadingProgressBar.Maximum = total;
                    LoadingDetailsText.Text = $"{current} / {total}";
                });
            };

            imageManager.ImportProgressChanged += (current, total, errors) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ImportProgressBar.Value = current;
                    ImportProgressBar.Maximum = total;
                    ImportDetailsText.Text = $"{current} / {total}" + (errors > 0 ? $" ({errors} errors)" : "");
                });
            };

            imageManager.LogMessage += msg => debugLogger.Log(msg);

            // ZoomController events
            zoomController.ZoomChanged += level => indicatorManager.ShowZoom(zoomController.ZoomPercent);
            zoomController.ZoomedIn += () => pauseController.Pause();
            zoomController.ZoomedOut += () => pauseController.Resume();
            zoomController.LogMessage += msg => debugLogger.Log(msg);

            // MosaicManager events
            mosaicManager.PaneCountChanged += paneCount =>
            {
                ShowImage(currentIndex);
                if (zoomController.IsZoomed)
                {
                    zoomController.ResetZoom();
                }
            };
            mosaicManager.LogMessage += msg => debugLogger.Log(msg);

            // SlideshowController events
            slideshowController.Tick += OnSlideshowTick;
            slideshowController.IntervalChanged += interval => indicatorManager.ShowSpeed(interval);
            slideshowController.LogMessage += msg => debugLogger.Log(msg);

            // PauseController events
            pauseController.Paused += () => slideshowController.Stop();
            pauseController.Resumed += () => slideshowController.Start();
            pauseController.LogMessage += msg => debugLogger.Log(msg);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize UI references
            debugLogger.Initialize(DebugConsole, LogTextBlock);
            pauseController.Initialize(PausePlayIcon, PauseBar1, PauseBar2, PlayTriangle);
            indicatorManager.Initialize(SpeedIndicator, SpeedText, ZoomIndicator, ZoomText);
            zoomController.Initialize(MosaicScaleTransform, MosaicTranslateTransform);

            debugLogger.Log("Application started");

            // Load images
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingProgressStack.Visibility = Visibility.Visible;

            await imageManager.LoadImagesAsync();

            if (imageManager.Images.Count > 0)
            {
                // Shuffle images
                ShuffleImages();
                ShowImage(0);
                slideshowController.Start();
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoadingText.Text = "No images found. Press I to import from '_' folders.";
            }
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging window when not in fullscreen and not zoomed
            if (!isFullscreen && !zoomController.IsZoomed)
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

        private void OnSlideshowTick()
        {
            currentIndex = (currentIndex + mosaicManager.PaneCount) % imageManager.Images.Count;
            mosaicManager.ResetPaneIndex();
            ShowImage(currentIndex);
        }

        private void ShowImage(int index)
        {
            if (imageManager.Images.Count > 0 && index >= 0 && index < imageManager.Images.Count)
            {
                // Prepare list of images to display based on mosaic pane count
                var imagesToShow = new List<BitmapImage>();
                for (int i = 0; i < mosaicManager.PaneCount; i++)
                {
                    int imageIndex = (index + i) % imageManager.Images.Count;
                    imagesToShow.Add(imageManager.Images[imageIndex]);
                }

                MosaicDisplay.ItemsSource = imagesToShow;

                // Update the grid layout
                var itemsPanel = FindVisualChild<UniformGrid>(MosaicDisplay);
                if (itemsPanel != null)
                {
                    mosaicManager.UpdateGridLayout(itemsPanel);
                }

                string logMsg = $"Showing: {imageManager.ImageFileNames[index]}";
                if (mosaicManager.PaneCount > 1)
                    logMsg += $" (+{mosaicManager.PaneCount - 1} more)";
                debugLogger.Log(logMsg);
            }
        }

        private void ShuffleImages()
        {
            // Not implemented in new architecture - would need to add to ImageManager
            // For now, images remain in original order
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Check for Ctrl+W or Ctrl+Q to close
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
                        imageManager.DeleteAllImages();
                    }
                }

                debugLogger.Log("Application closing");
                Application.Current.Shutdown();
                return;
            }

            // Check for Space or Enter to pause/resume
            if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                pauseController.Toggle();
                return;
            }

            switch (e.Key)
            {
                case Key.Up:
                case Key.Right:
                    NavigateNext();
                    break;

                case Key.Down:
                case Key.Left:
                    NavigatePrevious();
                    break;

                case Key.F:
                    ToggleFullscreen();
                    break;

                case Key.D:
                    debugLogger.Toggle();
                    break;

                case Key.OemComma: // < key
                case Key.OemPeriod: // > key
                    if (e.Key == Key.OemComma && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        slideshowController.IncreaseSpeed();
                        slideshowController.Restart();
                        if (pauseController.IsPaused) slideshowController.Stop();
                    }
                    else if (e.Key == Key.OemPeriod && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        slideshowController.DecreaseSpeed();
                        slideshowController.Restart();
                        if (pauseController.IsPaused) slideshowController.Stop();
                    }
                    break;

                case Key.OemMinus:
                case Key.OemPlus:
                    if (!mosaicManager.IsMosaicMode)
                    {
                        if (e.Key == Key.OemMinus)
                            zoomController.ZoomOut();
                        else
                            zoomController.ZoomIn();
                    }
                    break;

                case Key.M:
                    mosaicManager.IncreasePanes();
                    break;

                case Key.N:
                    mosaicManager.DecreasePanes();
                    break;

                case Key.I:
                    _ = ImportImagesAsync();
                    break;
            }
        }

        private void NavigateNext()
        {
            slideshowController.Stop();
            currentIndex = (currentIndex + mosaicManager.PaneCount) % imageManager.Images.Count;
            mosaicManager.ResetPaneIndex();
            ShowImage(currentIndex);
            FlashSide(true);
            if (!pauseController.IsPaused)
                slideshowController.Start();
        }

        private void NavigatePrevious()
        {
            slideshowController.Stop();
            currentIndex = (currentIndex - mosaicManager.PaneCount + imageManager.Images.Count) % imageManager.Images.Count;
            mosaicManager.ResetPaneIndex();
            ShowImage(currentIndex);
            FlashSide(false);
            if (!pauseController.IsPaused)
                slideshowController.Start();
        }

        private async void FlashSide(bool isRight)
        {
            var flash = isRight ? RightFlash : LeftFlash;
            flash.Opacity = 0.3;

            for (int i = 3; i >= 0; i--)
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

            int imported = await imageManager.ImportImagesAsync();

            if (imported > 0)
            {
                ImportProgressStack.Visibility = Visibility.Collapsed;
                await imageManager.LoadImagesAsync();
                
                if (imageManager.Images.Count > 0)
                {
                    ShuffleImages();
                    ShowImage(0);
                    slideshowController.Start();
                }
            }

            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        private void ToggleFullscreen()
        {
            if (!isFullscreen)
            {
                previousWindowState = WindowState;
                previousWindowStyle = WindowStyle;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                isFullscreen = true;
            }
            else
            {
                WindowStyle = previousWindowStyle;
                WindowState = previousWindowState;
                isFullscreen = false;
            }
        }

        // Zoom and Pan event handlers
        private void MosaicDisplay_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!mosaicManager.IsMosaicMode)
            {
                zoomController.HandleMouseWheel(e);
            }
        }

        private void MosaicDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!mosaicManager.IsMosaicMode && zoomController.IsZoomed)
            {
                zoomController.StartDrag(e.GetPosition(this));
                MosaicDisplay.CaptureMouse();
            }
        }

        private void MosaicDisplay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            zoomController.EndDrag();
            MosaicDisplay.ReleaseMouseCapture();
        }

        private void MosaicDisplay_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                zoomController.UpdateDrag(e.GetPosition(this));
            }
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
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