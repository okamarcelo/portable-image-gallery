using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace ImageGallery.Services;

/// <summary>
/// Manages lazy loading and caching of images with a sliding window strategy.
/// Keeps only a subset of images in memory to prevent excessive memory usage.
/// </summary>
public class ImageCache : IDisposable
{
    private readonly ILogger<ImageCache> _logger;
    private readonly ConcurrentDictionary<int, BitmapImage> _cache = new();
    private readonly ConcurrentDictionary<int, string> _imageFilePaths = new();
    private readonly int _cacheSize;
    private readonly int _preloadAhead;
    private readonly int _keepBehind;
    
    /// <summary>
    /// Total number of images available
    /// </summary>
    public int TotalImages => _imageFilePaths.Count;
    

    /// <summary>
    /// Creates a new image cache with the specified parameters.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="cacheSize">Maximum number of images to keep in memory (default: 64)</param>
    /// <param name="preloadAhead">Number of images to preload ahead of current position (default: 16)</param>
    /// <param name="keepBehind">Number of images to keep behind current position (default: 8)</param>
    public ImageCache(ILogger<ImageCache> logger, int cacheSize = 64, int preloadAhead = 16, int keepBehind = 8)
    {
        _logger = logger;
        _cacheSize = cacheSize;
        _preloadAhead = preloadAhead;
        _keepBehind = keepBehind;
        
        _logger.LogInformation("ImageCache initialized: size={CacheSize}, ahead={PreloadAhead}, behind={KeepBehind}", 
            cacheSize, preloadAhead, keepBehind);
    }

    /// <summary>
    /// Initialize the cache with the list of image file paths.
    /// Does not load images into memory yet.
    /// </summary>
    public void Initialize(IEnumerable<string> filePaths)
    {
        _imageFilePaths.Clear();
        _cache.Clear();
        
        var index = 0;
        foreach (var path in filePaths)
        {
            _imageFilePaths[index++] = path;
        }
        
        _logger.LogInformation("ImageCache initialized with {FilePathCount} file paths (0 images in memory)", _imageFilePaths.Count);
    }

    /// <summary>
    /// Get an image at the specified index, loading it if not in cache.
    /// </summary>
    public async Task<BitmapImage?> GetImageAsync(int index)
    {
        if (index < 0 || index >= _imageFilePaths.Count)
            return null;

        try
        {
            // Return from cache if available
            if (_cache.TryGetValue(index, out var cachedImage))
            {
                return cachedImage;
            }

            // Enforce cache size limit before adding new image
            if (_cache.Count >= _cacheSize)
            {
                // Remove oldest entry (first key)
                var oldestKey = _cache.Keys.First();
                _cache.TryRemove(oldestKey, out _);
                _logger.LogTrace("Cache full, evicted image {ImageIndex}", oldestKey);
            }

            // Load the image
            var image = await LoadImageAsync(_imageFilePaths[index]);
            if (image != null)
            {
                _cache.TryAdd(index, image);
                _logger.LogTrace("Loaded image {Index}: {FileName} (cache: {CacheCount}/{CacheSize})", 
                    index, Path.GetFileName(_imageFilePaths[index]), _cache.Count, _cacheSize);
            }

            return image;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FATAL ERROR in GetImageAsync({Index})", index);
            throw; // Re-throw to trigger global handler
        }
    }

    /// <summary>
    /// Get multiple images starting from the specified index.
    /// Useful for mosaic displays.
    /// </summary>
    public async Task<List<BitmapImage>> GetImagesAsync(int startIndex, int count)
    {
        var images = new List<BitmapImage>();
        
        for (var i = 0; i < count; i++)
        {
            var index = (startIndex + i) % _imageFilePaths.Count;
            var image = await GetImageAsync(index);
            if (image != null)
            {
                images.Add(image);
            }
        }
        
        return images;
    }

    /// <summary>
    /// Preload images around the current index using sliding window strategy.
    /// Evicts old images that are outside the window.
    /// </summary>
    public async Task PreloadWindowAsync(int currentIndex, int paneCount = 1)
    {
        if (_imageFilePaths.Count == 0)
            return;

        // Calculate window range
        var windowStart = Math.Max(0, currentIndex - _keepBehind);
        var windowEnd = Math.Min(_imageFilePaths.Count - 1, currentIndex + paneCount + _preloadAhead);
        
        // Evict images outside the window
        var keysToRemove = _cache.Keys
            .Where(k => k < windowStart || k > windowEnd)
            .ToList();
        
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
        
        if (keysToRemove.Count > 0)
        {
            _logger.LogTrace("Evicted {EvictedCount} images from cache (freed memory)", keysToRemove.Count);
        }

        // Preload images in the window that aren't cached
        var tasks = new List<Task>();
        for (var i = windowStart; i <= windowEnd; i++)
        {
            if (!_cache.ContainsKey(i))
            {
                var index = i; // Capture for closure
                tasks.Add(Task.Run(async () =>
                {
                    var image = await LoadImageAsync(_imageFilePaths[index]);
                    if (image != null)
                    {
                        // Only add if still within cache limit and not already present
                        if (!_cache.ContainsKey(index) && _cache.Count < _cacheSize)
                        {
                            _cache.TryAdd(index, image);
                        }
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);
        _logger.LogTrace("PreloadWindow: [{WindowStart}-{WindowEnd}], in cache: {CacheCount}/{CacheSize} images", 
            windowStart, windowEnd, _cache.Count, _cacheSize);
    }

    /// <summary>
    /// Get the filename for an image at the specified index.
    /// </summary>
    public string? GetFileName(int index) => _imageFilePaths.TryGetValue(index, out var path) ? Path.GetFileName(path) : null;


    /// <summary>
    /// Clear all cached images from memory.
    /// </summary>
    public Task ClearCacheAsync()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.LogInformation("Cleared {CachedImageCount} images from cache", count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shuffle the image file paths order.
    /// </summary>
    public void Shuffle(Random random)
    {
        if (_imageFilePaths.Count <= 1)
            return;

        var paths = _imageFilePaths.Values.ToList();
        
        // Fisher-Yates shuffle
        for (var i = paths.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (paths[i], paths[j]) = (paths[j], paths[i]);
        }

        _imageFilePaths.Clear();
        _cache.Clear();
        
        for (var i = 0; i < paths.Count; i++)
        {
            _imageFilePaths[i] = paths[i];
        }
        
        _logger.LogInformation("Shuffled {ImageCount} images, cache cleared - will reload on demand", _imageFilePaths.Count);
    }

    private async Task<BitmapImage?> LoadImageAsync(string filePath)
    {
        _logger.LogTrace("LoadImageAsync starting for: {FileName}", Path.GetFileName(filePath));
        return await Task.Run(() =>
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze(); // Make it thread-safe
                _logger.LogTrace("LoadImageAsync SUCCESS for: {FileName}", Path.GetFileName(filePath));
                return bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ERROR loading {FileName}", Path.GetFileName(filePath));
                return null;
            }
        });
    }

    public void Dispose()
    {
        _cache.Clear();
        _imageFilePaths.Clear();
    }
}

