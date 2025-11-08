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
        private readonly ImageCache _imageCache;
        private readonly string[] _supportedExtensions = { ".png", ".jpg", ".jpeg", ".heic", ".heif", ".webp" };
        private readonly Random _random = new();
        private string _folderPattern = "images";

        public int ImageCount => _imageCache.TotalImages;
        public string FolderPattern 
        { 
            get => _folderPattern; 
            set => _folderPattern = string.IsNullOrWhiteSpace(value) ? "" : value; 
        }
        
        public event Action<int, int>? LoadProgressChanged; // current, total
        public event Action<int, int, int>? ImportProgressChanged; // current, total, errorCount
        public event Action<string>? LogMessage;

        public ImageManager(ILogger<ImageManager> logger, ImageCache imageCache)
        {
            _logger = logger;
            _imageCache = imageCache;
        }

        /// <summary>
        /// Get images from cache for display. Used with lazy loading.
        /// </summary>
        public async Task<List<BitmapImage>> GetImagesAsync(int startIndex, int count)
        {
            _logger.LogTrace("GetImagesAsync called: startIndex={StartIndex}, count={Count}", 
                startIndex, count);
            


            return await _imageCache.GetImagesAsync(startIndex, count);
        }

        /// <summary>
        /// Preload images around current position for smoother playback.
        /// </summary>
        public async Task PreloadImagesAsync(int currentIndex, int paneCount) => await _imageCache.PreloadWindowAsync(currentIndex, paneCount);

        /// <summary>
        /// Get the filename for an image at the specified index.
        /// </summary>
        public string? GetImageFileName(int index) => _imageCache.GetFileName(index);

        /// <summary>
        /// Shuffle the loaded images randomly.
        /// </summary>
        public void Shuffle()
        {
   
                _imageCache.Shuffle(_random);
                _logger.LogInformation("Shuffled {ImageCount} images (lazy loading)", _imageCache.TotalImages);

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
                    var searchMessage = string.IsNullOrWhiteSpace(_folderPattern)
                        ? $"Searching for images in all subdirectories of {rootDirectory}..."
                        : $"Searching for '{_folderPattern}' folders in {rootDirectory}...";
                    _logger.LogInformation(searchMessage);

                    var matchingFolders = new List<string>();
                    var accessDeniedCount = 0;
                    FindMatchingFoldersRecursive(rootDirectory, matchingFolders, ref accessDeniedCount);

                    if (accessDeniedCount > 0)
                    {
                        _logger.LogError($"Access denied to {accessDeniedCount} folder(s)");
                    }

                    if (matchingFolders.Count == 0)
                    {
                        var notFoundMessage = string.IsNullOrWhiteSpace(_folderPattern)
                            ? "No subdirectories found"
                            : $"No '{_folderPattern}' folders found";
                        _logger.LogInformation(notFoundMessage);
                        return;
                    }

                    var foundMessage = string.IsNullOrWhiteSpace(_folderPattern)
                        ? $"Searching in {matchingFolders.Count} subdirectories"
                        : $"Found {matchingFolders.Count} '{_folderPattern}' folder(s)";
                    _logger.LogInformation(foundMessage);

                    // Collect all image files from matching folders
                    var allImageFiles = new List<string>();
                    foreach (var folder in matchingFolders)
                    {
                        try
                        {
                            var files = Directory.GetFiles(folder)
                                .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                                .ToList();
                            allImageFiles.AddRange(files);
                            _logger.LogInformation($"Found {files.Count} image(s) in {folder}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error reading folder {folder}: {ex.Message}");
                        }
                    }

                    if (allImageFiles.Count == 0)
                    {
                        var noImagesMessage = string.IsNullOrWhiteSpace(_folderPattern)
                            ? "No images found in subdirectories"
                            : $"No images found in '{_folderPattern}' folders";
                        LogMessage?.Invoke(noImagesMessage);
                        return;
                    }

                    _logger.LogTrace("Total image files found: {ImageFileCount}", allImageFiles.Count);
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

                        _logger.LogInformation("Lazy Loading: Found {ImageCount} images - initializing cache (not loading into memory)", allImageFiles.Count);
                        _imageCache.Initialize(allImageFiles);
                        
                        // Report progress as complete immediately since we're not loading actual images
                        LoadProgressChanged?.Invoke(allImageFiles.Count, allImageFiles.Count);
                        
                        var successMessage = string.IsNullOrWhiteSpace(_folderPattern)
                            ? $"[Lazy Loading] Ready with {allImageFiles.Count} images (0 loaded in memory, will load on-demand)"
                            : $"[Lazy Loading] Ready with {allImageFiles.Count} images from '{_folderPattern}' folders (0 loaded in memory)";
                        _logger.LogInformation(successMessage);

                   
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error loading images: {ex.Message}");
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
                    var matchingFolders = FindMatchingFolders(appDirectory);
                    if (matchingFolders == null || matchingFolders.Count == 0)
                        return;

                    var allFiles = CollectImageFiles(matchingFolders);
                    if (allFiles == null || allFiles.Count == 0)
                        return;

                    _logger.LogInformation($"Found {allFiles.Count} files to import");
                    ImportProgressChanged?.Invoke(0, allFiles.Count, 0);

                    // Import files with duplicate detection
                    var existingHashes = BuildExistingFileHashSet(appDirectory);
                    importedCount = ImportNewFiles(appDirectory, allFiles, existingHashes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during import");
                }
            });

            return importedCount;
        }

        /// <summary>
        /// Find all folders matching the configured pattern.
        /// </summary>
        private List<string>? FindMatchingFolders(string appDirectory)
        {
            var searchMessage = string.IsNullOrWhiteSpace(_folderPattern)
                ? "Searching for images in all subdirectories..."
                : $"Searching for '{_folderPattern}' folders...";
            _logger.LogInformation(searchMessage);

            var matchingFolders = new List<string>();
            var accessDeniedCount = 0;
            FindMatchingFoldersRecursive(appDirectory, matchingFolders, ref accessDeniedCount);

            if (accessDeniedCount > 0)
            {
                _logger.LogInformation($"Access denied to {accessDeniedCount} folder(s)");
            }

            if (matchingFolders.Count == 0)
            {
                var notFoundMessage = string.IsNullOrWhiteSpace(_folderPattern)
                    ? "No subdirectories found"
                    : $"No '{_folderPattern}' folders found";
                _logger.LogInformation(notFoundMessage);
                return null;
            }

            var foundMessage = string.IsNullOrWhiteSpace(_folderPattern)
                ? $"Found {matchingFolders.Count} subdirectories"
                : $"Found {matchingFolders.Count} '{_folderPattern}' folders";
            _logger.LogInformation(foundMessage);

            return matchingFolders;
        }

        /// <summary>
        /// Collect all image files from the specified folders.
        /// </summary>
        private List<string>? CollectImageFiles(List<string> folders)
        {
            var allFiles = new List<string>();
            foreach (var folder in folders)
            {
                try
                {
                    var files = Directory.GetFiles(folder)
                        .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .ToList();
                    allFiles.AddRange(files);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error reading folder {folder}: {ex.Message}");
                }
            }

            if (allFiles.Count == 0)
            {
                var noImagesMessage = string.IsNullOrWhiteSpace(_folderPattern)
                    ? "No images found in subdirectories"
                    : $"No images found in '{_folderPattern}' folders";
                _logger.LogInformation(noImagesMessage);
                return null;
            }

            return allFiles;
        }

        /// <summary>
        /// Build a hash set of existing files in the destination directory to detect duplicates.
        /// </summary>
        private HashSet<string> BuildExistingFileHashSet(string appDirectory)
        {
            var existingHashes = new HashSet<string>();
            var existingFiles = Directory.GetFiles(appDirectory)
                .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToList();

            foreach (var file in existingFiles)
            {
                try
                {
                    var hash = GetFileHash(file);
                    existingHashes.Add(hash);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to compute hash for existing file: {FileName}", Path.GetFileName(file));
                }
            }

            return existingHashes;
        }

        /// <summary>
        /// Import new files that don't already exist (based on hash comparison).
        /// </summary>
        private int ImportNewFiles(string appDirectory, List<string> allFiles, HashSet<string> existingHashes)
        {
            var importedCount = 0;
            var importErrors = 0;
            var existingFiles = Directory.GetFiles(appDirectory)
                .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToList();

            foreach (var sourceFile in allFiles)
            {
                try
                {
                    // Skip if source file is already in destination (avoid duplicate hash computation)
                    if (existingFiles.Contains(sourceFile))
                    {
                        continue;
                    }

                    var hash = GetFileHash(sourceFile);
                    if (!existingHashes.Contains(hash))
                    {
                        var extension = Path.GetExtension(sourceFile);
                        var newFileName = $"{Guid.NewGuid()}{extension}";
                        var destPath = Path.Combine(appDirectory, newFileName);
                        File.Copy(sourceFile, destPath, false);
                        existingHashes.Add(hash);
                        importedCount++;
                        _logger.LogInformation($"Imported: {Path.GetFileName(sourceFile)} -> {newFileName}");
                    }
                }
                catch (Exception ex)
                {
                    importErrors++;
                    _logger.LogError($"Error importing {Path.GetFileName(sourceFile)}: {ex.Message}");
                }

                ImportProgressChanged?.Invoke(importedCount + importErrors, allFiles.Count, importErrors);
            }

            _logger.LogInformation($"Import complete: {importedCount} files imported, {importErrors} errors");
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
                    .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLower()));

                foreach (var file in imageFiles)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogInformation($"Deleted: {Path.GetFileName(file)}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error deleting {Path.GetFileName(file)}: {ex.Message}");
                    }
                }

                _logger.LogInformation("All images deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting images: {ex.Message}");
            }
        }

        private void FindMatchingFoldersRecursive(string directory, List<string> results, ref int accessDeniedCount)
        {
            try
            {
                foreach (var subDir in Directory.GetDirectories(directory))
                {
                    // If pattern is empty, add all subdirectories; otherwise match the pattern
                    if (string.IsNullOrWhiteSpace(_folderPattern))
                    {
                        results.Add(subDir);
                    }
                    else if (Path.GetFileName(subDir) == _folderPattern)
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
                _logger.LogError($"Error scanning {directory}: {ex.Message}");
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
