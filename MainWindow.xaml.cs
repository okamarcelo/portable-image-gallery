using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ImageGallery
{
    public partial class MainWindow : Window
    {
        private List<BitmapImage> images = new List<BitmapImage>();
        private List<string> imageFileNames = new List<string>();
        private int currentIndex = 0;
        private bool isFullscreen = false;
        private bool isPaused = false;
        private WindowState previousWindowState;
        private WindowStyle previousWindowStyle;
        
        private DispatcherTimer slideshowTimer;
        private DispatcherTimer blinkTimer;
        private double slideshowInterval = 5.0; // seconds (now double for .5 increments)
        private StringBuilder logBuilder = new StringBuilder();
        
        private int mosaicPaneCount = 1; // 1, 2, 4, 9, or 16
        private int[] mosaicSizes = { 1, 2, 4, 9, 16 };
        private int currentMosaicPaneIndex = 0; // Track which pane to update in mosaic mode
        
        // Zoom state
        private double zoomLevel = 1.0;
        private Point panOffset = new Point(0, 0);
        private Point lastMousePosition;
        private bool isDraggingImage = false;

        public MainWindow()
        {
            InitializeComponent();
            
            // Setup slideshow timer
            slideshowTimer = new DispatcherTimer();
            slideshowTimer.Tick += SlideshowTimer_Tick;
            UpdateTimerInterval();
            
            // Setup blink timer for pause/play icon
            blinkTimer = new DispatcherTimer();
            blinkTimer.Interval = TimeSpan.FromMilliseconds(800);
            blinkTimer.Tick += BlinkTimer_Tick;
            
            // Setup mouse down for window dragging
            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging window when not in fullscreen and not zoomed
            if (!isFullscreen && zoomLevel <= 1.0)
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log("Application started");
            _ = LoadImagesAsync();
        }

        private void Log(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            logBuilder.AppendLine(timestampedMessage);
            LogTextBlock.Text = logBuilder.ToString();
            
            // Auto-scroll to bottom
            if (DebugConsole.Visibility == Visibility.Visible)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(DebugConsole);
                scrollViewer?.ScrollToEnd();
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

        private async Task LoadImagesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Get the directory where the executable is located
                    string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    
                    Dispatcher.Invoke(() => Log($"Scanning directory: {exeDirectory}"));
                    
                    // Supported image extensions
                    string[] supportedExtensions = { "*.png", "*.jpg", "*.jpeg", "*.heic", "*.heif", "*.webp" };
                    
                    List<string> imageFiles = new List<string>();
                    
                    foreach (string extension in supportedExtensions)
                    {
                        imageFiles.AddRange(Directory.GetFiles(exeDirectory, extension, SearchOption.TopDirectoryOnly));
                    }
                    
                    Dispatcher.Invoke(() => 
                    {
                        Log($"Found {imageFiles.Count} image(s)");
                        LoadingProgressBar.Maximum = imageFiles.Count > 0 ? imageFiles.Count : 1;
                    });
                    
                    // Shuffle the list for random order
                    Random rng = new Random();
                    imageFiles = imageFiles.OrderBy(x => rng.Next()).ToList();
                    
                    // Load images into memory
                    int loadedCount = 0;
                    foreach (string file in imageFiles)
                    {
                        try
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(file);
                            bitmap.EndInit();
                            bitmap.Freeze(); // For performance and cross-thread access
                            
                            Dispatcher.Invoke(() =>
                            {
                                images.Add(bitmap);
                                imageFileNames.Add(Path.GetFileName(file));
                                loadedCount++;
                                LoadingProgressBar.Value = loadedCount;
                                LoadingDetailsText.Text = $"{loadedCount} / {imageFiles.Count}";
                                Log($"Loaded [{loadedCount}/{imageFiles.Count}]: {Path.GetFileName(file)}");
                            });
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() => 
                            {
                                loadedCount++;
                                LoadingProgressBar.Value = loadedCount;
                                LoadingDetailsText.Text = $"{loadedCount} / {imageFiles.Count}";
                                Log($"Failed to load {Path.GetFileName(file)}: {ex.Message}");
                            });
                        }
                    }
                    
                    Dispatcher.Invoke(() =>
                    {
                        Log($"Loading complete. {images.Count} image(s) ready.");
                        
                        // Hide loading overlay
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                        
                        if (images.Count > 0)
                        {
                            ShowImage(currentIndex);
                            slideshowTimer.Start();
                        }
                        else
                        {
                            // No images found - offer to run import script
                            var result = MessageBox.Show(
                                "No images found in this directory.\n\n" +
                                "Would you like to import images from '_' folders?",
                                "No Images Found",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);
                            
                            if (result == MessageBoxResult.Yes)
                            {
                                RunImageImport();
                            }
                            else
                            {
                                Application.Current.Shutdown();
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Log($"Error loading images: {ex.Message}");
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                        MessageBox.Show($"Error loading images: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        private void SlideshowTimer_Tick(object? sender, EventArgs e)
        {
            // Auto-advance by pane count
            currentIndex = (currentIndex + mosaicPaneCount) % images.Count;
            currentMosaicPaneIndex = 0; // Reset pane index
            ShowImage(currentIndex);
        }

        private void UpdateTimerInterval()
        {
            slideshowTimer.Interval = TimeSpan.FromSeconds(slideshowInterval);
            Log($"Slideshow interval set to {slideshowInterval:0.0} second(s)");
            ShowSpeedIndicator();
        }

        private async void ShowSpeedIndicator()
        {
            SpeedText.Text = $"{slideshowInterval:0.0}s";
            SpeedIndicator.Visibility = Visibility.Visible;
            
            // Blink effect - fade in and out
            for (int i = 0; i < 3; i++)
            {
                // Fade in
                for (double opacity = 0; opacity <= 0.8; opacity += 0.1)
                {
                    SpeedIndicator.Opacity = opacity;
                    await Task.Delay(30);
                }
                
                // Fade out
                for (double opacity = 0.8; opacity >= 0; opacity -= 0.1)
                {
                    SpeedIndicator.Opacity = opacity;
                    await Task.Delay(30);
                }
            }
            
            SpeedIndicator.Visibility = Visibility.Collapsed;
            SpeedIndicator.Opacity = 1.0; // Reset for next time
        }

        private void ShowImage(int index)
        {
            if (images.Count > 0 && index >= 0 && index < images.Count)
            {
                // Prepare list of images to display based on mosaic pane count
                var imagesToShow = new List<BitmapImage>();
                for (int i = 0; i < mosaicPaneCount; i++)
                {
                    int imageIndex = (index + i) % images.Count;
                    imagesToShow.Add(images[imageIndex]);
                }
                
                MosaicDisplay.ItemsSource = imagesToShow;
                
                // Update the grid layout
                UpdateMosaicLayout();
                
                Log($"Showing: {imageFileNames[index]}" + (mosaicPaneCount > 1 ? $" (+{mosaicPaneCount - 1} more)" : ""));
            }
        }
        
        private void UpdateMosaicLayout()
        {
            // Find the UniformGrid in the ItemsPanel
            var itemsPanel = FindVisualChild<UniformGrid>(MosaicDisplay);
            if (itemsPanel != null)
            {
                // Handle special case for 2 panes (1x2 layout)
                if (mosaicPaneCount == 2)
                {
                    itemsPanel.Rows = 1;
                    itemsPanel.Columns = 2;
                }
                else
                {
                    int gridSize = (int)Math.Sqrt(mosaicPaneCount);
                    itemsPanel.Rows = gridSize;
                    itemsPanel.Columns = gridSize;
                }
            }
        }


        private void BlinkTimer_Tick(object? sender, EventArgs e)
        {
            // Toggle opacity for blinking effect
            if (isPaused)
            {
                PauseBar1.Opacity = PauseBar1.Opacity == 0.6 ? 0.3 : 0.6;
                PauseBar2.Opacity = PauseBar2.Opacity == 0.6 ? 0.3 : 0.6;
            }
            else
            {
                PlayTriangle.Opacity = PlayTriangle.Opacity == 0.6 ? 0.3 : 0.6;
            }
        }

        private async void FlashSide(bool isRight)
        {
            var flash = isRight ? RightFlash : LeftFlash;
            
            // Fade in
            for (double i = 0; i <= 0.3; i += 0.05)
            {
                flash.Opacity = i;
                await Task.Delay(10);
            }
            
            // Fade out
            for (double i = 0.3; i >= 0; i -= 0.05)
            {
                flash.Opacity = i;
                await Task.Delay(10);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Check for Ctrl+W or Ctrl+Q to close
            if ((e.Key == Key.W || e.Key == Key.Q) && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.Q)
                {
                    // Ctrl+Q: Prompt to delete all images
                    var result = MessageBox.Show(
                        "Do you want to delete all image files in this directory?",
                        "Delete Images",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        DeleteAllImages();
                    }
                }
                
                Log("Application closing");
                Application.Current.Shutdown();
                return;
            }
            
            // Check for Space or Enter to pause/resume
            if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                TogglePause();
                return;
            }
            
            switch (e.Key)
            {
                case Key.Up:
                case Key.Right:
                    // Next image - restart timer and flash right side
                    slideshowTimer.Stop();
                    currentIndex = (currentIndex + mosaicPaneCount) % images.Count;
                    currentMosaicPaneIndex = 0; // Reset pane index
                    ShowImage(currentIndex);
                    FlashSide(true);
                    if (!isPaused)
                        slideshowTimer.Start();
                    break;
                    
                case Key.Down:
                case Key.Left:
                    // Previous image - restart timer and flash left side
                    slideshowTimer.Stop();
                    currentIndex = (currentIndex - mosaicPaneCount + images.Count) % images.Count;
                    currentMosaicPaneIndex = 0; // Reset pane index
                    ShowImage(currentIndex);
                    FlashSide(false);
                    if (!isPaused)
                        slideshowTimer.Start();
                    break;
                    
                case Key.F:
                    // Toggle fullscreen
                    ToggleFullscreen();
                    break;
                    
                case Key.D:
                    // Toggle debug console
                    ToggleDebugConsole();
                    break;
                    
                case Key.OemComma: // < key
                case Key.OemPeriod: // > key
                    if (e.Key == Key.OemComma && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        // Shift+< increases speed (inverted)
                        slideshowInterval += 0.5;
                        UpdateTimerInterval();
                        slideshowTimer.Stop();
                        if (!isPaused)
                            slideshowTimer.Start();
                    }
                    else if (e.Key == Key.OemPeriod && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        // Shift+> decreases speed (inverted)
                        if (slideshowInterval > 0.5)
                        {
                            slideshowInterval -= 0.5;
                            UpdateTimerInterval();
                            slideshowTimer.Stop();
                            if (!isPaused)
                                slideshowTimer.Start();
                        }
                    }
                    break;
                    
                case Key.OemMinus:
                case Key.OemPlus:
                    // Zoom controls
                    if (mosaicPaneCount == 1)
                    {
                        if (e.Key == Key.OemMinus) // - or _
                        {
                            ZoomOut();
                        }
                        else if (e.Key == Key.OemPlus) // + or =
                        {
                            ZoomIn();
                        }
                    }
                    break;
                    
                case Key.M:
                    // Increase mosaic pane count
                    IncreaseMosaicPanes();
                    break;
                    
                case Key.N:
                    // Decrease mosaic pane count
                    DecreaseMosaicPanes();
                    break;
            }
        }
        
        private void IncreaseMosaicPanes()
        {
            int currentSizeIndex = Array.IndexOf(mosaicSizes, mosaicPaneCount);
            int nextIndex = (currentSizeIndex + 1) % mosaicSizes.Length;
            mosaicPaneCount = mosaicSizes[nextIndex];
            ShowImage(currentIndex);
            Log($"Mosaic mode: {mosaicPaneCount} pane{(mosaicPaneCount > 1 ? "s" : "")}");
        }
        
        private void DecreaseMosaicPanes()
        {
            int currentSizeIndex = Array.IndexOf(mosaicSizes, mosaicPaneCount);
            int prevIndex = (currentSizeIndex - 1 + mosaicSizes.Length) % mosaicSizes.Length;
            mosaicPaneCount = mosaicSizes[prevIndex];
            ShowImage(currentIndex);
            Log($"Mosaic mode: {mosaicPaneCount} pane{(mosaicPaneCount > 1 ? "s" : "")}");
        }

        private void TogglePause()
        {
            isPaused = !isPaused;
            
            if (isPaused)
            {
                slideshowTimer.Stop();
                PausePlayIcon.Visibility = Visibility.Visible;
                PauseBar1.Visibility = Visibility.Visible;
                PauseBar2.Visibility = Visibility.Visible;
                PlayTriangle.Visibility = Visibility.Collapsed;
                blinkTimer.Start();
                Log("Slideshow paused");
            }
            else
            {
                slideshowTimer.Start();
                blinkTimer.Stop();
                
                // Show play icon briefly
                PauseBar1.Visibility = Visibility.Collapsed;
                PauseBar2.Visibility = Visibility.Collapsed;
                PlayTriangle.Visibility = Visibility.Visible;
                
                // Hide icon after 1 second
                Task.Delay(1000).ContinueWith(_ => 
                {
                    Dispatcher.Invoke(() => PausePlayIcon.Visibility = Visibility.Collapsed);
                });
                
                Log("Slideshow resumed");
            }
        }

        private void DeleteAllImages()
        {
            try
            {
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                
                string[] supportedExtensions = { "*.png", "*.jpg", "*.jpeg", "*.heic", "*.heif", "*.webp" };
                
                int deletedCount = 0;
                foreach (string extension in supportedExtensions)
                {
                    var files = Directory.GetFiles(exeDirectory, extension, SearchOption.TopDirectoryOnly);
                    foreach (string file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                            Log($"Deleted: {Path.GetFileName(file)}");
                        }
                        catch (Exception ex)
                        {
                            Log($"Failed to delete {Path.GetFileName(file)}: {ex.Message}");
                        }
                    }
                }
                
                Log($"Deleted {deletedCount} image file(s)");
            }
            catch (Exception ex)
            {
                Log($"Error deleting images: {ex.Message}");
            }
        }

        private void RunImageImport()
        {
            try
            {
                // Create input dialog for source path
                var sourceDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select SOURCE directory (contains '_' folders)",
                    ShowNewFolderButton = false
                };
                
                if (sourceDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    Application.Current.Shutdown();
                    return;
                }
                
                string sourcePath = sourceDialog.SelectedPath;
                string destPath = AppDomain.CurrentDomain.BaseDirectory; // Always use app directory
                
                Log($"Starting import from: {sourcePath}");
                Log($"Destination: {destPath}");
                
                // Show loading overlay with import progress
                LoadingOverlay.Visibility = Visibility.Visible;
                LoadingText.Text = "Importing and loading images...";
                ImportProgressStack.Visibility = Visibility.Visible;
                LoadingProgressStack.Visibility = Visibility.Visible;
                ImportProgressBar.Value = 0;
                ImportDetailsText.Text = "Searching for '_' folders...";
                LoadingProgressBar.Value = 0;
                LoadingDetailsText.Text = "Waiting for import...";
                
                _ = ImportImagesAsync(sourcePath, destPath);
            }
            catch (Exception ex)
            {
                Log($"Error setting up import: {ex.Message}");
                Application.Current.Shutdown();
            }
        }

        private async Task ImportImagesAsync(string sourcePath, string destPath)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Find all "_" folders recursively with error handling
                    List<string> underscoreFolders = new List<string>();
                    int accessErrors = 0;
                    
                    try
                    {
                        FindUnderscoreFoldersRecursive(sourcePath, underscoreFolders, ref accessErrors);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => Log($"Error searching directories: {ex.Message}"));
                    }
                    
                    string errorMsg = accessErrors > 0 ? $" ({accessErrors} access denied)" : "";
                    
                    Dispatcher.Invoke(() =>
                    {
                        Log($"Found {underscoreFolders.Count} '_' folder(s){errorMsg}");
                        ImportDetailsText.Text = $"Found {underscoreFolders.Count} '_' folders{errorMsg}";
                    });
                    
                    if (underscoreFolders.Count == 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Log("No '_' folders found");
                            ImportProgressStack.Visibility = Visibility.Collapsed;
                            LoadingDetailsText.Text = "No '_' folders found";
                        });
                        return;
                    }
                    
                    // Collect all files
                    List<string> allFiles = new List<string>();
                    int fileAccessErrors = 0;
                    
                    foreach (var folder in underscoreFolders)
                    {
                        try
                        {
                            var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
                            allFiles.AddRange(files);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            fileAccessErrors++;
                            Dispatcher.Invoke(() => Log($"Access denied: {folder}"));
                        }
                        catch (Exception ex)
                        {
                            fileAccessErrors++;
                            Dispatcher.Invoke(() => Log($"Error accessing folder {folder}: {ex.Message}"));
                        }
                    }
                    
                    string fileErrorMsg = fileAccessErrors > 0 ? $" ({fileAccessErrors} folders skipped)" : "";
                    
                    Dispatcher.Invoke(() =>
                    {
                        Log($"Found {allFiles.Count} file(s) to process{fileErrorMsg}");
                        ImportProgressBar.Maximum = allFiles.Count > 0 ? allFiles.Count : 1;
                        ImportDetailsText.Text = $"Processing {allFiles.Count} files{fileErrorMsg}";
                    });
                    
                    int processedCount = 0;
                    int copiedCount = 0;
                    int skippedCount = 0;
                    int failedCount = 0;
                    
                    foreach (var file in allFiles)
                    {
                        try
                        {
                            string extension = Path.GetExtension(file);
                            string uuid = Guid.NewGuid().ToString();
                            string newFileName = uuid + extension;
                            string destFilePath = Path.Combine(destPath, newFileName);
                            
                            // Check if file already exists with same hash
                            bool shouldCopy = true;
                            if (File.Exists(destFilePath))
                            {
                                try
                                {
                                    string sourceHash = GetFileHash(file);
                                    string destHash = GetFileHash(destFilePath);
                                    
                                    if (sourceHash == destHash)
                                    {
                                        shouldCopy = false;
                                        skippedCount++;
                                        Dispatcher.Invoke(() => Log($"Skipped (duplicate): {Path.GetFileName(file)}"));
                                    }
                                }
                                catch (Exception hashEx)
                                {
                                    Dispatcher.Invoke(() => Log($"Hash error for {Path.GetFileName(file)}: {hashEx.Message}"));
                                    failedCount++;
                                }
                            }
                            
                            if (shouldCopy)
                            {
                                try
                                {
                                    File.Copy(file, destFilePath, true);
                                    copiedCount++;
                                    Dispatcher.Invoke(() => Log($"Copied: {Path.GetFileName(file)} ? {newFileName}"));
                                }
                                catch (Exception copyEx)
                                {
                                    Dispatcher.Invoke(() => Log($"Copy failed for {Path.GetFileName(file)}: {copyEx.Message}"));
                                    failedCount++;
                                }
                            }
                            
                            processedCount++;
                            Dispatcher.Invoke(() =>
                            {
                                ImportProgressBar.Value = processedCount;
                                ImportDetailsText.Text = $"{processedCount} / {allFiles.Count} processed";
                            });
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() => Log($"Error processing {Path.GetFileName(file)}: {ex.Message}"));
                            failedCount++;
                            processedCount++;
                        }
                    }
                    
                    Dispatcher.Invoke(() =>
                    {
                        Log($"Import complete: {copiedCount} copied, {skippedCount} skipped, {failedCount} failed");
                        ImportProgressStack.Visibility = Visibility.Collapsed;
                        
                        // Now start loading images after import is complete
                        LoadingDetailsText.Text = "Starting image loading...";
                        _ = LoadImagesAsync();
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Log($"Import error: {ex.Message}");
                        ImportProgressStack.Visibility = Visibility.Collapsed;
                    });
                }
            });
        }

        private void FindUnderscoreFoldersRecursive(string currentPath, List<string> underscoreFolders, ref int accessErrors)
        {
            try
            {
                // Check if current directory is named "_"
                if (Path.GetFileName(currentPath) == "_")
                {
                    underscoreFolders.Add(currentPath);
                }
                
                // Get subdirectories and search them recursively
                try
                {
                    var subdirs = Directory.GetDirectories(currentPath);
                    foreach (var subdir in subdirs)
                    {
                        FindUnderscoreFoldersRecursive(subdir, underscoreFolders, ref accessErrors);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    accessErrors++;
                    Dispatcher.Invoke(() => Log($"Access denied: {currentPath}"));
                }
                catch (Exception ex)
                {
                    accessErrors++;
                    Dispatcher.Invoke(() => Log($"Error accessing: {currentPath} - {ex.Message}"));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"Error processing: {currentPath} - {ex.Message}"));
            }
        }

        private string GetFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        private void ToggleDebugConsole()
        {
            if (DebugConsole.Visibility == Visibility.Visible)
            {
                DebugConsole.Visibility = Visibility.Collapsed;
            }
            else
            {
                DebugConsole.Visibility = Visibility.Visible;
                var scrollViewer = FindVisualChild<ScrollViewer>(DebugConsole);
                scrollViewer?.ScrollToEnd();
            }
        }

        private void ToggleFullscreen()
        {
            if (!isFullscreen)
            {
                // Enter fullscreen
                previousWindowState = WindowState;
                previousWindowStyle = WindowStyle;
                
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                isFullscreen = true;
            }
            else
            {
                // Exit fullscreen
                WindowStyle = previousWindowStyle;
                WindowState = previousWindowState;
                isFullscreen = false;
            }
        }
        
        // Zoom and Pan functionality
        private void ZoomIn()
        {
            if (mosaicPaneCount > 1) return; // Only in single image mode
            
            zoomLevel += 0.1;
            ApplyZoom();
            
            // Pause slideshow when zoomed
            if (zoomLevel > 1.0 && !isPaused)
            {
                TogglePause();
            }
        }
        
        private void ZoomOut()
        {
            if (mosaicPaneCount > 1) return; // Only in single image mode
            
            if (zoomLevel > 1.0)
            {
                zoomLevel -= 0.1;
                if (zoomLevel < 1.0) zoomLevel = 1.0;
                ApplyZoom();
                
                // Resume slideshow when back to original size
                if (zoomLevel <= 1.0 && isPaused)
                {
                    panOffset = new Point(0, 0);
                    MosaicTranslateTransform.X = 0;
                    MosaicTranslateTransform.Y = 0;
                    TogglePause();
                }
            }
        }
        
        private async void ApplyZoom()
        {
            MosaicScaleTransform.ScaleX = zoomLevel;
            MosaicScaleTransform.ScaleY = zoomLevel;
            
            // Show zoom indicator
            int zoomPercent = (int)(zoomLevel * 100);
            ZoomText.Text = $"{zoomPercent}%";
            ZoomIndicator.Visibility = Visibility.Visible;
            Log($"Zoom: {zoomPercent}%");
            
            // Blink animation (fainter than speed indicator)
            for (int i = 0; i < 3; i++)
            {
                await Task.Delay(150);
                double opacity = ZoomIndicator.Opacity == 0.4 ? 0.2 : 0.4;
                ZoomIndicator.Opacity = opacity;
            }
            
            await Task.Delay(500);
            ZoomIndicator.Visibility = Visibility.Collapsed;
            ZoomIndicator.Opacity = 0.4;
        }
        
        private void MosaicDisplay_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (mosaicPaneCount > 1) return; // Only in single image mode
            
            if (e.Delta > 0)
            {
                ZoomIn();
            }
            else
            {
                ZoomOut();
            }
        }
        
        private void MosaicDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (mosaicPaneCount == 1 && zoomLevel > 1.0)
            {
                isDraggingImage = true;
                lastMousePosition = e.GetPosition(this);
                MosaicDisplay.CaptureMouse();
            }
        }
        
        private void MosaicDisplay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDraggingImage)
            {
                isDraggingImage = false;
                MosaicDisplay.ReleaseMouseCapture();
            }
        }
        
        private void MosaicDisplay_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingImage && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(this);
                Vector delta = currentPosition - lastMousePosition;
                
                panOffset.X += delta.X;
                panOffset.Y += delta.Y;
                
                MosaicTranslateTransform.X = panOffset.X;
                MosaicTranslateTransform.Y = panOffset.Y;
                
                lastMousePosition = currentPosition;
            }
        }
    }
}
