using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageGallery.Services;
using Xunit;

namespace ImageGallery.Tests.Services;

/// <summary>
/// Tests for ImageCache lazy loading and sliding window functionality
/// </summary>
public class ImageCacheTests : IDisposable
{
    private readonly string testImageDir;
    private readonly List<string> testImagePaths;
    private const int TestImageCount = 100;

    public ImageCacheTests()
    {
        // Create temp directory with test image files
        testImageDir = Path.Combine(Path.GetTempPath(), $"ImageGalleryTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(testImageDir);

        testImagePaths = new List<string>();
        for (var i = 0; i < TestImageCount; i++)
        {
            var path = Path.Combine(testImageDir, $"test_image_{i:D3}.jpg");
            // Create a minimal valid JPEG file (1x1 pixel red image)
            File.WriteAllBytes(path, CreateMinimalJpeg());
            testImagePaths.Add(path);
        }
    }

    public void Dispose()
    {
        // Cleanup test files
        try
        {
            if (Directory.Exists(testImageDir))
            {
                Directory.Delete(testImageDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void Constructor_SetsParametersCorrectly()
    {
        // Arrange & Act
        using var cache = new ImageCache(cacheSize: 50, preloadAhead: 10, keepBehind: 5);

        // Assert
        Assert.Equal(0, cache.TotalImages);
    }

    [Fact]
    public void Initialize_StoresFilePathsWithoutLoadingImages()
    {
        // Arrange
        using var cache = new ImageCache();
        var logMessages = new List<string>();
        cache.LogMessage += msg => logMessages.Add(msg);

        // Act
        cache.Initialize(testImagePaths);

        // Assert
        Assert.Equal(TestImageCount, cache.TotalImages);
        Assert.Contains(logMessages, msg => msg.Contains($"Initialized with {TestImageCount} file paths"));
        Assert.Contains(logMessages, msg => msg.Contains("0 images in memory"));
    }

    [Fact]
    public async Task GetImageAsync_LoadsImageOnDemand()
    {
        // Arrange
        using var cache = new ImageCache(cacheSize: 10);
        cache.Initialize(testImagePaths);
        var logMessages = new List<string>();
        cache.LogMessage += msg => logMessages.Add(msg);

        // Act
        var image = await cache.GetImageAsync(0);

        // Assert
        Assert.NotNull(image);
        Assert.Contains(logMessages, msg => msg.Contains("LoadImageAsync starting"));
        Assert.Contains(logMessages, msg => msg.Contains("LoadImageAsync SUCCESS"));
        Assert.Contains(logMessages, msg => msg.Contains("Loaded image 0"));
        Assert.Contains(logMessages, msg => msg.Contains("cache: 1/10"));
    }

    [Fact]
    public async Task GetImageAsync_ReturnsCachedImageOnSecondCall()
    {
        // Arrange
        using var cache = new ImageCache(cacheSize: 10);
        cache.Initialize(testImagePaths);
        var logMessages = new List<string>();
        
        await cache.GetImageAsync(0); // First load
        logMessages.Clear();
        cache.LogMessage += msg => logMessages.Add(msg);

        // Act
        var image = await cache.GetImageAsync(0); // Second load (from cache)

        // Assert
        Assert.NotNull(image);
        Assert.DoesNotContain(logMessages, msg => msg.Contains("LoadImageAsync starting"));
    }

    [Fact]
    public async Task GetImageAsync_EvictsOldestWhenCacheFull()
    {
        // Arrange
        using var cache = new ImageCache(cacheSize: 3);
        cache.Initialize(testImagePaths);
        var logMessages = new List<string>();
        cache.LogMessage += msg => logMessages.Add(msg);

        // Act - Fill cache with images 0, 1, 2
        await cache.GetImageAsync(0);
        await cache.GetImageAsync(1);
        await cache.GetImageAsync(2);
        
        logMessages.Clear();
        
        // Load image 3 - should evict image 0
        await cache.GetImageAsync(3);

        // Assert
        Assert.Contains(logMessages, msg => msg.Contains("Cache full, evicted image"));
    }

    [Fact]
    public async Task GetImagesAsync_LoadsMultipleImages()
    {
        // Arrange
        using var cache = new ImageCache(cacheSize: 20);
        cache.Initialize(testImagePaths);

        // Act
        var images = await cache.GetImagesAsync(startIndex: 5, count: 3);

        // Assert
        Assert.Equal(3, images.Count);
        Assert.All(images, img => Assert.NotNull(img));
    }

    [Fact]
    public async Task GetImagesAsync_HandlesWrapAround()
    {
        // Arrange
        using var cache = new ImageCache(cacheSize: 20);
        cache.Initialize(testImagePaths.Take(10).ToList());

        // Act - Request images wrapping around the end
        var images = await cache.GetImagesAsync(startIndex: 8, count: 4);

        // Assert
        Assert.Equal(4, images.Count);
        Assert.All(images, img => Assert.NotNull(img));
    }

    [Fact]
    public async Task PreloadWindowAsync_LoadsImagesInWindow()
    {
        // Arrange
        using var cache = new ImageCache(cacheSize: 64, preloadAhead: 5, keepBehind: 3);
        cache.Initialize(testImagePaths);
        var logMessages = new List<string>();
        cache.LogMessage += msg => logMessages.Add(msg);

        // Act
        await cache.PreloadWindowAsync(currentIndex: 10, paneCount: 1);

        // Assert - Should log window range and cache count
        // Window: currentIndex - keepBehind to currentIndex + paneCount + preloadAhead - 1
        // 10 - 3 = 7, 10 + 1 + 5 - 1 = 15 (but formula is currentIndex + paneCount + preloadAhead)
        Assert.Contains(logMessages, msg => msg.Contains("Window:") && msg.Contains("in cache:"));
    }

    [Fact]
    public async Task PreloadWindowAsync_EvictsImagesOutsideWindow()
    {
        // Arrange
        using var cache = new ImageCache(cacheSize: 64, preloadAhead: 5, keepBehind: 3);
        cache.Initialize(testImagePaths);
        
        // Load images at position 0
        await cache.PreloadWindowAsync(currentIndex: 0, paneCount: 1);
        
        var logMessages = new List<string>();
        cache.LogMessage += msg => logMessages.Add(msg);

        // Act - Move to position 50 (far away, should evict old images)
        await cache.PreloadWindowAsync(currentIndex: 50, paneCount: 1);

        // Assert
        Assert.Contains(logMessages, msg => msg.Contains("Evicted") && msg.Contains("images from cache"));
    }

    [Fact]
    public async Task PreloadWindowAsync_RespectsMaxCacheSize()
    {
        // Arrange
        using var cache = new ImageCache(cacheSize: 10, preloadAhead: 20, keepBehind: 20);
        cache.Initialize(testImagePaths);
        var logMessages = new List<string>();
        cache.LogMessage += msg => logMessages.Add(msg);

        // Act
        await cache.PreloadWindowAsync(currentIndex: 50, paneCount: 1);

        // Assert - Even with large window, should respect cache size limit
        var cacheCountMessages = logMessages.Where(msg => msg.Contains("in cache:")).ToList();
        if (cacheCountMessages.Any())
        {
            var lastMessage = cacheCountMessages.Last();
            // Extract cache count (format: "in cache: X/10 images")
            var parts = lastMessage.Split("in cache: ")[1].Split('/');
            var cacheCount = int.Parse(parts[0].Trim());
            Assert.True(cacheCount <= 10, $"Cache exceeded limit: {cacheCount} > 10");
        }
    }

    [Fact]
    public void Shuffle_ReordersImages()
    {
        // Arrange
        using var cache = new ImageCache();
        cache.Initialize(testImagePaths);
        var originalFirstFile = cache.GetFileName(0);

        // Act
        cache.Shuffle(new Random(42)); // Fixed seed for reproducibility

        // Assert
        var newFirstFile = cache.GetFileName(0);
        // With 100 images and shuffle, very unlikely to have same first file
        Assert.NotEqual(originalFirstFile, newFirstFile);
    }

    [Fact]
    public void Shuffle_ClearsCache()
    {
        // Arrange
        using var cache = new ImageCache();
        cache.Initialize(testImagePaths);
        var logMessages = new List<string>();
        cache.LogMessage += msg => logMessages.Add(msg);

        // Act
        cache.Shuffle(new Random());

        // Assert
        Assert.Contains(logMessages, msg => msg.Contains("cache cleared"));
    }

    [Fact]
    public async Task ClearCacheAsync_RemovesAllCachedImages()
    {
        // Arrange
        using var cache = new ImageCache(cacheSize: 20);
        cache.Initialize(testImagePaths);
        
        // Load some images
        await cache.GetImageAsync(0);
        await cache.GetImageAsync(1);
        await cache.GetImageAsync(2);
        
        var logMessages = new List<string>();
        cache.LogMessage += msg => logMessages.Add(msg);

        // Act
        await cache.ClearCacheAsync();

        // Assert
        Assert.Contains(logMessages, msg => msg.Contains("Cleared") && msg.Contains("images from cache"));
    }

    [Fact]
    public async Task ConcurrentAccess_HandlesMultipleSimultaneousRequests()
    {
        // Arrange
        using var cache = new ImageCache(cacheSize: 50);
        cache.Initialize(testImagePaths);

        // Act - Request multiple images concurrently
        var tasks = new List<Task<System.Windows.Media.Imaging.BitmapImage?>>();
        for (var i = 0; i < 20; i++)
        {
            var index = i; // Capture for closure
            tasks.Add(Task.Run(() => cache.GetImageAsync(index)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(20, results.Length);
        Assert.All(results, img => Assert.NotNull(img));
    }

    [Fact]
    public async Task GetImageAsync_ReturnsNullForInvalidIndex()
    {
        // Arrange
        using var cache = new ImageCache();
        cache.Initialize(testImagePaths);

        // Act
        var negativeResult = await cache.GetImageAsync(-1);
        var tooLargeResult = await cache.GetImageAsync(TestImageCount + 10);

        // Assert
        Assert.Null(negativeResult);
        Assert.Null(tooLargeResult);
    }

    [Fact]
    public void GetFileName_ReturnsCorrectFileName()
    {
        // Arrange
        using var cache = new ImageCache();
        cache.Initialize(testImagePaths);

        // Act
        var fileName = cache.GetFileName(5);

        // Assert
        Assert.Equal("test_image_005.jpg", fileName);
    }

    [Fact]
    public void GetFileName_ReturnsNullForInvalidIndex()
    {
        // Arrange
        using var cache = new ImageCache();
        cache.Initialize(testImagePaths);

        // Act
        var result = cache.GetFileName(-1);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Creates a minimal valid 1x1 pixel red JPEG file
    /// </summary>
    private byte[] CreateMinimalJpeg()
    {
        // This is a base64-encoded 1x1 red JPEG
        var base64 = @"/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0a
HBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIy
MjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIA
AhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQA
AAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3
ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWm
p6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEA
AwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSEx
BhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElK
U1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlbaWmJmaoqOkpaanqKmqsrO0tba3
uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD3+iii
gD//2Q==";
        return Convert.FromBase64String(base64.Replace("\r", "").Replace("\n", ""));
    }
}
