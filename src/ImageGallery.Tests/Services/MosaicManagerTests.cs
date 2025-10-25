using System;
using ImageGallery.Services;
using Xunit;

namespace ImageGallery.Tests.Services;

public class MosaicManagerTests
{
    [Fact]
    public void Constructor_InitializesWithOnePaneCount()
    {
        // Arrange & Act
        var manager = new MosaicManager();

        // Assert
        Assert.Equal(1, manager.PaneCount);
    }

    [Fact]
    public void Constructor_InitializesAsNotMosaicMode()
    {
        // Arrange & Act
        var manager = new MosaicManager();

        // Assert
        Assert.False(manager.IsMosaicMode);
    }

    [Fact]
    public void IncreasePanes_CyclesThroughSizes()
    {
        // Arrange
        var manager = new MosaicManager();
        int[] expectedSizes = { 2, 4, 9, 16, 1 };

        // Act & Assert
        foreach (var expectedSize in expectedSizes)
        {
            manager.IncreasePanes();
            Assert.Equal(expectedSize, manager.PaneCount);
        }
    }

    [Fact]
    public void DecreasePanes_CyclesThroughSizesInReverse()
    {
        // Arrange
        var manager = new MosaicManager();
        int[] expectedSizes = { 16, 9, 4, 2, 1 };

        // Act & Assert
        foreach (var expectedSize in expectedSizes)
        {
            manager.DecreasePanes();
            Assert.Equal(expectedSize, manager.PaneCount);
        }
    }

    [Fact]
    public void IsMosaicMode_TrueWhenPaneCountGreaterThanOne()
    {
        // Arrange
        var manager = new MosaicManager();

        // Act
        manager.IncreasePanes(); // Should be 2

        // Assert
        Assert.True(manager.IsMosaicMode);
    }

    [Fact]
    public void IsMosaicMode_FalseWhenPaneCountIsOne()
    {
        // Arrange
        var manager = new MosaicManager();
        manager.IncreasePanes(); // Move to 2
        manager.DecreasePanes(); // Back to 1

        // Assert
        Assert.False(manager.IsMosaicMode);
    }

    [Fact]
    public void PaneCountChanged_EventFiresOnIncrease()
    {
        // Arrange
        var manager = new MosaicManager();
        int? newPaneCount = null;
        manager.PaneCountChanged += (count) => newPaneCount = count;

        // Act
        manager.IncreasePanes();

        // Assert
        Assert.NotNull(newPaneCount);
        Assert.Equal(2, newPaneCount.Value);
    }

    [Fact]
    public void PaneCountChanged_EventFiresOnDecrease()
    {
        // Arrange
        var manager = new MosaicManager();
        int? newPaneCount = null;
        manager.PaneCountChanged += (count) => newPaneCount = count;

        // Act
        manager.DecreasePanes();

        // Assert
        Assert.NotNull(newPaneCount);
        Assert.Equal(16, newPaneCount.Value);
    }

    [Fact]
    public void LogMessage_EventFiresOnIncreasePanes()
    {
        // Arrange
        var manager = new MosaicManager();
        string? logMessage = null;
        manager.LogMessage += (msg) => logMessage = msg;

        // Act
        manager.IncreasePanes();

        // Assert
        Assert.NotNull(logMessage);
        Assert.Contains("Mosaic", logMessage);
        Assert.Contains("2", logMessage);
    }

    [Fact]
    public void LogMessage_EventFiresOnDecreasePanes()
    {
        // Arrange
        var manager = new MosaicManager();
        string? logMessage = null;
        manager.LogMessage += (msg) => logMessage = msg;

        // Act
        manager.DecreasePanes();

        // Assert
        Assert.NotNull(logMessage);
        Assert.Contains("Mosaic", logMessage);
        Assert.Contains("16", logMessage);
    }

    // Note: UpdateGridLayout tests are skipped because they require WPF UI components
    // which need STA thread context that's difficult to set up in unit tests.
    // This method is tested through integration tests or manual testing.
}
