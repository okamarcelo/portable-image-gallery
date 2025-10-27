using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ImageGallery.Services;

/// <summary>
/// Manages lazy loading and caching of images with a sliding window strategy.
/// Keeps only a subset of images in memory to prevent excessive memory usage.
/// </summary>
public class ImageCache : IDisposable
{
    private readonly Dictionary<int, BitmapImage> cache = new();
    private readonly Dictionary<int, string> imageFilePaths = new();
    private readonly SemaphoreSlim cacheLock = new(1, 1);
    private readonly int cacheSize;
    private readonly int preloadAhead;
    private readonly int keepBehind;
    
    /// <summary>
    /// Total number of images available
    /// </summary>
    public int TotalImages => imageFilePaths.Count;
    
    public event Action<string>? LogMessage;

    /// <summary>
    /// Creates a new image cache with the specified parameters.
    /// </summary>
    /// <param name="cacheSize">Maximum number of images to keep in memory (default: 32)</param>
    /// <param name="preloadAhead">Number of images to preload ahead of current position (default: 16)</param>
    /// <param name="keepBehind">Number of images to keep behind current position (default: 8)</param>
    public ImageCache(int cacheSize = 32, int preloadAhead = 16, int keepBehind = 8)
    {
        this.cacheSize = cacheSize;
        this.preloadAhead = preloadAhead;
        this.keepBehind = keepBehind;
        
        LogMessage?.Invoke($"ImageCache initialized: size={cacheSize}, ahead={preloadAhead}, behind={keepBehind}");
    }

    /// <summary>
    /// Initialize the cache with the list of image file paths.
    /// Does not load images into memory yet.
    /// </summary>
    public void Initialize(IEnumerable<string> filePaths)
    {
        imageFilePaths.Clear();
        cache.Clear();
        
        int index = 0;
        foreach (var path in filePaths)
        {
            imageFilePaths[index++] = path;
        }
        
        LogMessage?.Invoke($"Cache initialized with {imageFilePaths.Count} image paths");
    }

    /// <summary>
    /// Get an image at the specified index, loading it if not in cache.
    /// </summary>
    public async Task<BitmapImage?> GetImageAsync(int index)
    {
        if (index < 0 || index >= imageFilePaths.Count)
            return null;

        await cacheLock.WaitAsync();
        try
        {
            // Return from cache if available
            if (cache.TryGetValue(index, out var cachedImage))
            {
                return cachedImage;
            }

            // Load the image
            var image = await LoadImageAsync(imageFilePaths[index]);
            if (image != null)
            {
                cache[index] = image;
                LogMessage?.Invoke($"Loaded image {index}: {Path.GetFileName(imageFilePaths[index])}");
            }

            return image;
        }
        finally
        {
            cacheLock.Release();
        }
    }

    /// <summary>
    /// Get multiple images starting from the specified index.
    /// Useful for mosaic displays.
    /// </summary>
    public async Task<List<BitmapImage>> GetImagesAsync(int startIndex, int count)
    {
        var images = new List<BitmapImage>();
        
        for (int i = 0; i < count; i++)
        {
            int index = (startIndex + i) % imageFilePaths.Count;
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
        if (imageFilePaths.Count == 0)
            return;

        await cacheLock.WaitAsync();
        try
        {
            // Calculate window range
            int windowStart = Math.Max(0, currentIndex - keepBehind);
            int windowEnd = Math.Min(imageFilePaths.Count - 1, currentIndex + paneCount + preloadAhead);
            
            // Evict images outside the window
            var keysToRemove = cache.Keys
                .Where(k => k < windowStart || k > windowEnd)
                .ToList();
            
            foreach (var key in keysToRemove)
            {
                cache.Remove(key);
            }
            
            if (keysToRemove.Count > 0)
            {
                LogMessage?.Invoke($"Evicted {keysToRemove.Count} images from cache");
            }

            // Preload images in the window that aren't cached
            var tasks = new List<Task>();
            for (int i = windowStart; i <= windowEnd; i++)
            {
                if (!cache.ContainsKey(i))
                {
                    int index = i; // Capture for closure
                    tasks.Add(Task.Run(async () =>
                    {
                        var image = await LoadImageAsync(imageFilePaths[index]);
                        if (image != null)
                        {
                            await cacheLock.WaitAsync();
                            try
                            {
                                cache[index] = image;
                            }
                            finally
                            {
                                cacheLock.Release();
                            }
                        }
                    }));
                }
            }

            await Task.WhenAll(tasks);
            
            LogMessage?.Invoke($"Cache window: [{windowStart}-{windowEnd}], cached: {cache.Count}/{cacheSize}");
        }
        finally
        {
            cacheLock.Release();
        }
    }

    /// <summary>
    /// Get the filename for an image at the specified index.
    /// </summary>
    public string? GetFileName(int index)
    {
        if (imageFilePaths.TryGetValue(index, out var path))
        {
            return Path.GetFileName(path);
        }
        return null;
    }

    /// <summary>
    /// Clear all cached images from memory.
    /// </summary>
    public async Task ClearCacheAsync()
    {
        await cacheLock.WaitAsync();
        try
        {
            int count = cache.Count;
            cache.Clear();
            LogMessage?.Invoke($"Cleared {count} images from cache");
        }
        finally
        {
            cacheLock.Release();
        }
    }

    /// <summary>
    /// Shuffle the image file paths order.
    /// </summary>
    public void Shuffle(Random random)
    {
        if (imageFilePaths.Count <= 1)
            return;

        var paths = imageFilePaths.Values.ToList();
        
        // Fisher-Yates shuffle
        for (int i = paths.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (paths[i], paths[j]) = (paths[j], paths[i]);
        }

        imageFilePaths.Clear();
        cache.Clear();
        
        for (int i = 0; i < paths.Count; i++)
        {
            imageFilePaths[i] = paths[i];
        }
        
        LogMessage?.Invoke($"Shuffled {imageFilePaths.Count} images, cache cleared");
    }

    private async Task<BitmapImage?> LoadImageAsync(string filePath)
    {
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
                return bitmap;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Error loading {Path.GetFileName(filePath)}: {ex.Message}");
                return null;
            }
        });
    }

    public void Dispose()
    {
        cache.Clear();
        imageFilePaths.Clear();
        cacheLock.Dispose();
    }
}
