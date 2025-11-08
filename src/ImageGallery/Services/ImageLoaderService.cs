using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ImageGallery.Resources;

namespace ImageGallery.Services
{
    public class ImageLoaderService
    {
        private readonly ILogger<ImageLoaderService> _logger;
        private readonly ImageManager _imageManager;
        private readonly MosaicManager _mosaicManager;
        private readonly SlideshowController _slideshowController;
        private readonly WindowStateService _windowStateService;

        // UI elements references
        private Visibility _loadingOverlayVisibility;
        private Visibility _loadingProgressStackVisibility;
        private Visibility _importProgressStackVisibility;
        private string _loadingText = string.Empty;

        // Events for UI updates
        public event Action<Visibility>? LoadingOverlayVisibilityChanged;
        public event Action<Visibility>? LoadingProgressStackVisibilityChanged;
        public event Action<Visibility>? ImportProgressStackVisibilityChanged;
        public event Action<string>? LoadingTextChanged;
        public event Action? ShuffleImagesRequested;
        public event Action<int>? ShowImageRequested;
        public event Action? SlideshowStartRequested;

        public ImageLoaderService(
            ILogger<ImageLoaderService> logger,
            ImageManager imageManager,
            MosaicManager mosaicManager,
            SlideshowController slideshowController,
            WindowStateService windowStateService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _imageManager = imageManager ?? throw new ArgumentNullException(nameof(imageManager));
            _mosaicManager = mosaicManager ?? throw new ArgumentNullException(nameof(mosaicManager));
            _slideshowController = slideshowController ?? throw new ArgumentNullException(nameof(slideshowController));
            _windowStateService = windowStateService ?? throw new ArgumentNullException(nameof(windowStateService));
        }

        public async Task LoadFromCliArgumentsAsync(CommandLineArguments cliArgs)
        {
            try
            {
                if (cliArgs == null || !cliArgs.IsCliMode)
                    return;

                _logger.LogInformation(Strings.SLog_LoadingFromCLI);

                // Set folder pattern (empty string means all subdirectories)
                _imageManager.FolderPattern = cliArgs.FolderPattern ?? "";
                
                // Set pane count if specified
                if (cliArgs.PaneCount.HasValue)
                {
                    // Find the closest valid mosaic size
                    var targetPanes = cliArgs.PaneCount.Value;
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
                if (cliArgs.Fullscreen)
                {
                    // Note: We need to set fullscreen after the window is fully loaded
                    // So we'll do it after loading images
                    _logger.LogInformation(Strings.SLog_FullscreenModeWillBeActivated);
                }

                // Load images from specified directory
                SetLoadingOverlayVisibility(Visibility.Visible);
                SetLoadingProgressStackVisibility(Visibility.Visible);
                
                var patternText = string.IsNullOrWhiteSpace(_imageManager.FolderPattern)
                    ? Strings.Pattern_AllSubdirectories
                    : string.Format(Strings.Pattern_SpecificFolders, _imageManager.FolderPattern);
                SetLoadingText(string.Format(Strings.Loading_ImagesFrom, patternText));

                await _imageManager.LoadImagesFromDirectoryAsync(cliArgs.RootDirectory!);
                
                if (_imageManager.ImageCount > 0)
                {
                    ShuffleImagesRequested?.Invoke();
                    ShowImageRequested?.Invoke(0);
                    SlideshowStartRequested?.Invoke();
                    SetLoadingOverlayVisibility(Visibility.Collapsed);
                    
                    // Activate fullscreen mode if requested via CLI
                    if (cliArgs.Fullscreen)
                    {
                        _windowStateService.ToggleFullscreen();
                        _logger.LogInformation(Strings.SLog_ActivatedFullscreenMode);
                    }
                    
                    _logger.LogInformation(string.Format(Strings.SLog_CLIModeLoadedImages, _imageManager.ImageCount));
                }
                else
                {
                    SetLoadingText(string.Format(Strings.Error_NoImagesInPattern, patternText));
                    SetLoadingProgressStackVisibility(Visibility.Collapsed);
                    _logger.LogWarning(Strings.SLog_CLIModeNoImages);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Strings.SLog_ErrorLoadingFromCLI);
                SetLoadingOverlayVisibility(Visibility.Collapsed);
                MessageBox.Show(string.Format(Strings.Error_LoadImagesMessage, ex.Message), 
                    Strings.Error_LoadTitle, 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ImportImagesAsync()
        {
            SetLoadingOverlayVisibility(Visibility.Visible);
            SetLoadingText("Importing and loading images...");
            SetImportProgressStackVisibility(Visibility.Visible);
            SetLoadingProgressStackVisibility(Visibility.Visible);

            var imported = await _imageManager.ImportImagesAsync();

            if (imported > 0)
            {
                SetImportProgressStackVisibility(Visibility.Collapsed);
                await _imageManager.LoadImagesAsync();
                
                if (_imageManager.ImageCount > 0)
                {
                    ShuffleImagesRequested?.Invoke();
                    ShowImageRequested?.Invoke(0);
                    SlideshowStartRequested?.Invoke();
                }
            }

            SetLoadingOverlayVisibility(Visibility.Collapsed);
        }

        public async Task SelectRootDirectoryAsync()
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
                        SetLoadingText("No pattern specified. Press I to select a directory.");
                        SetLoadingProgressStackVisibility(Visibility.Collapsed);
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
                    
                    SetLoadingOverlayVisibility(Visibility.Visible);
                    SetLoadingText($"Searching for images in {patternText}...");
                    SetLoadingProgressStackVisibility(Visibility.Visible);

                    await _imageManager.LoadImagesFromDirectoryAsync(selectedPath);
                    
                    if (_imageManager.ImageCount > 0)
                    {
                        ShuffleImagesRequested?.Invoke();
                        ShowImageRequested?.Invoke(0);
                        SlideshowStartRequested?.Invoke();
                        SetLoadingOverlayVisibility(Visibility.Collapsed);
                        _logger.LogInformation($"Loaded {_imageManager.ImageCount} images from selected directory");
                    }
                    else
                    {
                        SetLoadingText($"No images found in {patternText}. Press I to try another directory.");
                        SetLoadingProgressStackVisibility(Visibility.Collapsed);
                        _logger.LogWarning("No images found in selected directory");
                    }
                }
                else
                {
                    _logger.LogInformation("User cancelled folder selection");
                    if (_imageManager.ImageCount == 0)
                    {
                        SetLoadingText("No directory selected. Press I to select a directory.");
                        SetLoadingProgressStackVisibility(Visibility.Collapsed);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting directory");
                MessageBox.Show($"Error selecting directory:\n{ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                SetLoadingOverlayVisibility(Visibility.Collapsed);
            }
        }

        private void SetLoadingOverlayVisibility(Visibility visibility)
        {
            _loadingOverlayVisibility = visibility;
            LoadingOverlayVisibilityChanged?.Invoke(visibility);
        }

        private void SetLoadingProgressStackVisibility(Visibility visibility)
        {
            _loadingProgressStackVisibility = visibility;
            LoadingProgressStackVisibilityChanged?.Invoke(visibility);
        }

        private void SetImportProgressStackVisibility(Visibility visibility)
        {
            _importProgressStackVisibility = visibility;
            ImportProgressStackVisibilityChanged?.Invoke(visibility);
        }

        private void SetLoadingText(string text)
        {
            _loadingText = text;
            LoadingTextChanged?.Invoke(text);
        }
    }
}