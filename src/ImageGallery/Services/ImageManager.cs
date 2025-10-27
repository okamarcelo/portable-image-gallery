using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace ImageGallery.Services;

/// <summary>
/// Manages image loading, importing, and file operations with lazy loading support.
/// Single Responsibility: Handle all image-related file operations and coordinate with cache.
/// </summary>
public class ImageManager
    {
        private readonly ILogger<ImageManager> _logger;
        private readonly ImageCache imageCache;
        private readonly string[] supportedExtensions = { ".png", ".jpg", ".jpeg", ".heic", ".heif", ".webp" };
        private readonly Random random = new Random();
        private string folderPattern = "images"; // Default pattern
        private bool useLazyLoading = true;

        // Legacy support - will be empty when using lazy loading
        private readonly List<BitmapImage> images = new List<BitmapImage>();
        private readonly List<string> imageFileNames = new List<string>();

        public IReadOnlyList<BitmapImage> Images => images.AsReadOnly();
        public IReadOnlyList<string> ImageFileNames => imageFileNames.AsReadOnly();
        public int ImageCount => useLazyLoading ? imageCache.TotalImages : images.Count;
        public bool UseLazyLoading 
        { 
            get => useLazyLoading; 
            set => useLazyLoading = value; 
        }
        
        public string FolderPattern 
        { 
            get => folderPattern; 
            set => folderPattern = string.IsNullOrWhiteSpace(value) ? "" : value; 
        }
        
        public event Action<int, int>? LoadProgressChanged; // current, total
        public event Action<int, int, int>? ImportProgressChanged; // current, total, errorCount
        public event Action<string>? LogMessage;

        public ImageManager(ILogger<ImageManager> logger, ImageCache imageCache)
        {
            _logger = logger;
            this.imageCache = imageCache;
            imageCache.LogMessage += msg => LogMessage?.Invoke(msg);
        }

        /// <summary>
        /// Get images from cache for display. Used with lazy loading.
        /// </summary>
        public async Task<List<BitmapImage>> GetImagesAsync(int startIndex, int count)
        {
            _logger.LogTrace("GetImagesAsync called: startIndex={StartIndex}, count={Count}, useLazyLoading={UseLazyLoading}", 
                startIndex, count, useLazyLoading);
            
            if (!useLazyLoading)
            {
                // Legacy mode - return from memory
                var result = new List<BitmapImage>();
                for (var i = 0; i < count; i++)
                {
                    var index = (startIndex + i) % images.Count;
                    if (index >= 0 && index < images.Count)
                        result.Add(images[index]);
                }
                return result;
            }

            return await imageCache.GetImagesAsync(startIndex, count);
        }

        /// <summary>
        /// Preload images around current position for smoother playback.
        /// </summary>
        public async Task PreloadImagesAsync(int currentIndex, int paneCount)
        {
            if (useLazyLoading)
            {
                await imageCache.PreloadWindowAsync(currentIndex, paneCount);
            }
        }

        /// <summary>
        /// Get the filename for an image at the specified index.
        /// </summary>
        public string? GetImageFileName(int index)
        {
            if (!useLazyLoading)
            {
                return index >= 0 && index < imageFileNames.Count ? imageFileNames[index] : null;
            }

            return imageCache.GetFileName(index);
        }

        /// <summary>
        /// Shuffle the loaded images randomly.
        /// </summary>
        public void Shuffle()
        {
            if (useLazyLoading)
            {
                imageCache.Shuffle(random);
                _logger.LogInformation("Shuffled {ImageCount} images (lazy loading)", imageCache.TotalImages);
                LogMessage?.Invoke($"Shuffled {imageCache.TotalImages} images (lazy loading)");
                return;
            }

            if (images.Count <= 1) return;

            // Fisher-Yates shuffle algorithm
            for (var i = images.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                
                // Swap images
                (images[i], images[j]) = (images[j], images[i]);
                
                // Swap filenames
                (imageFileNames[i], imageFileNames[j]) = (imageFileNames[j], imageFileNames[i]);
            }

            _logger.LogInformation("Shuffled {ImageCount} images", images.Count);
            LogMessage?.Invoke($"Shuffled {images.Count} images");
        }

        /// <summary>
        /// Load all images from folders matching the pattern found recursively in the application directory.
        /// </summary>
        public async Task LoadImagesAsync()
        {
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            await LoadImagesFromDirectoryAsync(appDirectory);
        }

        /// <summary>
        /// Load all images from folders matching the pattern found recursively in the specified directory.
        /// </summary>
        public async Task LoadImagesFromDirectoryAsync(string rootDirectory)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Clear existing images
                    images.Clear();
                    imageFileNames.Clear();

                    var searchMessage = string.IsNullOrWhiteSpace(folderPattern)
                        ? $"Searching for images in all subdirectories of {rootDirectory}..."
                        : $"Searching for '{folderPattern}' folders in {rootDirectory}...";
                    LogMessage?.Invoke(searchMessage);

                    var matchingFolders = new List<string>();
                    var accessDeniedCount = 0;
                    FindMatchingFoldersRecursive(rootDirectory, matchingFolders, ref accessDeniedCount);

                    if (accessDeniedCount > 0)
                    {
                        LogMessage?.Invoke($"Access denied to {accessDeniedCount} folder(s)");
                    }

                    if (matchingFolders.Count == 0)
                    {
                        var notFoundMessage = string.IsNullOrWhiteSpace(folderPattern)
                            ? "No subdirectories found"
                            : $"No '{folderPattern}' folders found";
                        LogMessage?.Invoke(notFoundMessage);
                        return;
                    }

                    var foundMessage = string.IsNullOrWhiteSpace(folderPattern)
                        ? $"Searching in {matchingFolders.Count} subdirectories"
                        : $"Found {matchingFolders.Count} '{folderPattern}' folder(s)";
                    LogMessage?.Invoke(foundMessage);

                    // Collect all image files from matching folders
                    var allImageFiles = new List<string>();
                    foreach (var folder in matchingFolders)
                    {
                        try
                        {
                            var files = Directory.GetFiles(folder)
                                .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                                .ToList();
                            allImageFiles.AddRange(files);
                            LogMessage?.Invoke($"Found {files.Count} image(s) in {folder}");
                        }
                        catch (Exception ex)
                        {
                            LogMessage?.Invoke($"Error reading folder {folder}: {ex.Message}");
                        }
                    }

                    if (allImageFiles.Count == 0)
                    {
                        var noImagesMessage = string.IsNullOrWhiteSpace(folderPattern)
                            ? "No images found in subdirectories"
                            : $"No images found in '{folderPattern}' folders";
                        LogMessage?.Invoke(noImagesMessage);
                        return;
                    }

                    _logger.LogTrace("Total image files found: {ImageFileCount}", allImageFiles.Count);
                    LogMessage?.Invoke($"[VERBOSE] Total image files found: {allImageFiles.Count}");
                    
                    _logger.LogTrace("Image files list (first 50):");
                    var count = Math.Min(50, allImageFiles.Count);
                    for (var i = 0; i < count; i++)
                    {
                        _logger.LogTrace("  {Index}. {FileName}", i + 1, allImageFiles[i]);
                    }
                    if (allImageFiles.Count > 50)
                    {
                        _logger.LogTrace("  ... and {RemainingCount} more files", allImageFiles.Count - 50);
                    }

                    if (useLazyLoading)
                    {
                        // Lazy loading mode - just initialize cache with file paths (no images loaded yet!)
                        _logger.LogInformation("Lazy Loading: Found {ImageCount} images - initializing cache (not loading into memory)", allImageFiles.Count);
                        LogMessage?.Invoke($"[Lazy Loading] Found {allImageFiles.Count} images - initializing cache (not loading into memory)");
                        imageCache.Initialize(allImageFiles);
                        
                        // Report progress as complete immediately since we're not loading actual images
                        LoadProgressChanged?.Invoke(allImageFiles.Count, allImageFiles.Count);
                        
                        var successMessage = string.IsNullOrWhiteSpace(folderPattern)
                            ? $"[Lazy Loading] Ready with {allImageFiles.Count} images (0 loaded in memory, will load on-demand)"
                            : $"[Lazy Loading] Ready with {allImageFiles.Count} images from '{folderPattern}' folders (0 loaded in memory)";
                        LogMessage?.Invoke(successMessage);
                    }
                    else
                    {
                        // Legacy mode - load all images into memory
                        LogMessage?.Invoke($"Loading {allImageFiles.Count} image files into memory...");
                        LoadProgressChanged?.Invoke(0, allImageFiles.Count);

                        var loadedCount = 0;
                        foreach (var file in allImageFiles)
                        {
                            try
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.UriSource = new Uri(file);
                                bitmap.EndInit();
                                bitmap.Freeze();

                                images.Add(bitmap);
                                imageFileNames.Add(Path.GetFileName(file));
                                loadedCount++;
                                LoadProgressChanged?.Invoke(loadedCount, allImageFiles.Count);
                            }
                            catch (Exception ex)
                            {
                                LogMessage?.Invoke($"Error loading {Path.GetFileName(file)}: {ex.Message}");
                                loadedCount++;
                                LoadProgressChanged?.Invoke(loadedCount, allImageFiles.Count);
                            }
                        }

                        var successMessage = string.IsNullOrWhiteSpace(folderPattern)
                            ? $"Successfully loaded {images.Count} images from subdirectories"
                            : $"Successfully loaded {images.Count} images from '{folderPattern}' folders";
                        LogMessage?.Invoke(successMessage);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"Error loading images: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Import images from all folders matching the pattern found recursively.
        /// </summary>
        public async Task<int> ImportImagesAsync()
        {
            var importedCount = 0;
            
            await Task.Run(() =>
            {
                try
                {
                    var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    var searchMessage = string.IsNullOrWhiteSpace(folderPattern)
                        ? "Searching for images in all subdirectories..."
                        : $"Searching for '{folderPattern}' folders...";
                    LogMessage?.Invoke(searchMessage);

                    var matchingFolders = new List<string>();
                    var accessDeniedCount = 0;
                    FindMatchingFoldersRecursive(appDirectory, matchingFolders, ref accessDeniedCount);

                    if (accessDeniedCount > 0)
                    {
                        LogMessage?.Invoke($"Access denied to {accessDeniedCount} folder(s)");
                    }

                    if (matchingFolders.Count == 0)
                    {
                        var notFoundMessage = string.IsNullOrWhiteSpace(folderPattern)
                            ? "No subdirectories found"
                            : $"No '{folderPattern}' folders found";
                        LogMessage?.Invoke(notFoundMessage);
                        return;
                    }

                    var foundMessage = string.IsNullOrWhiteSpace(folderPattern)
                        ? $"Found {matchingFolders.Count} subdirectories"
                        : $"Found {matchingFolders.Count} '{folderPattern}' folders";
                    LogMessage?.Invoke(foundMessage);

                    // Collect all image files
                    var allFiles = new List<string>();
                    foreach (var folder in matchingFolders)
                    {
                        try
                        {
                            var files = Directory.GetFiles(folder)
                                .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                                .ToList();
                            allFiles.AddRange(files);
                        }
                        catch (Exception ex)
                        {
                            LogMessage?.Invoke($"Error reading folder {folder}: {ex.Message}");
                        }
                    }

                    if (allFiles.Count == 0)
                    {
                        var noImagesMessage = string.IsNullOrWhiteSpace(folderPattern)
                            ? "No images found in subdirectories"
                            : $"No images found in '{folderPattern}' folders";
                        LogMessage?.Invoke(noImagesMessage);
                        return;
                    }

                    LogMessage?.Invoke($"Found {allFiles.Count} files to import");
                    ImportProgressChanged?.Invoke(0, allFiles.Count, 0);

                    // Import files with duplicate detection
                    var existingHashes = new HashSet<string>();
                    var existingFiles = Directory.GetFiles(appDirectory)
                        .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()));

                    foreach (var file in existingFiles)
                    {
                        try
                        {
                            var hash = GetFileHash(file);
                            existingHashes.Add(hash);
                        }
                        catch { }
                    }

                    var importErrors = 0;
                    foreach (var sourceFile in allFiles)
                    {
                        try
                        {
                            var hash = GetFileHash(sourceFile);
                            if (!existingHashes.Contains(hash))
                            {
                                var extension = Path.GetExtension(sourceFile);
                                var newFileName = $"{Guid.NewGuid()}{extension}";
                                var destPath = Path.Combine(appDirectory, newFileName);
                                File.Copy(sourceFile, destPath, false);
                                existingHashes.Add(hash);
                                importedCount++;
                                LogMessage?.Invoke($"Imported: {Path.GetFileName(sourceFile)} -> {newFileName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            importErrors++;
                            LogMessage?.Invoke($"Error importing {Path.GetFileName(sourceFile)}: {ex.Message}");
                        }

                        ImportProgressChanged?.Invoke(importedCount + importErrors, allFiles.Count, importErrors);
                    }

                    LogMessage?.Invoke($"Import complete: {importedCount} files imported, {importErrors} errors");
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"Error during import: {ex.Message}");
                }
            });

            return importedCount;
        }

        /// <summary>
        /// Delete all image files in the application directory.
        /// </summary>
        public void DeleteAllImages()
        {
            try
            {
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var imageFiles = Directory.GetFiles(appDirectory)
                    .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()));

                foreach (var file in imageFiles)
                {
                    try
                    {
                        File.Delete(file);
                        LogMessage?.Invoke($"Deleted: {Path.GetFileName(file)}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage?.Invoke($"Error deleting {Path.GetFileName(file)}: {ex.Message}");
                    }
                }

                LogMessage?.Invoke("All images deleted");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Error deleting images: {ex.Message}");
            }
        }

        private void FindMatchingFoldersRecursive(string directory, List<string> results, ref int accessDeniedCount)
        {
            try
            {
                foreach (var subDir in Directory.GetDirectories(directory))
                {
                    // If pattern is empty, add all subdirectories; otherwise match the pattern
                    if (string.IsNullOrWhiteSpace(folderPattern))
                    {
                        results.Add(subDir);
                    }
                    else if (Path.GetFileName(subDir) == folderPattern)
                    {
                        results.Add(subDir);
                    }
                    
                    FindMatchingFoldersRecursive(subDir, results, ref accessDeniedCount);
                }
            }
            catch (UnauthorizedAccessException)
            {
                accessDeniedCount++;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Error scanning {directory}: {ex.Message}");
            }
        }

    private string GetFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
