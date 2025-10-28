using System;
using System.Windows.Controls.Primitives;
using ImageGallery.Resources;

namespace ImageGallery.Services;

/// <summary>
/// Manages mosaic display configuration and layout.
/// Single Responsibility: Handle mosaic pane count and grid layout.
/// </summary>
public class MosaicManager
{
    private int _mosaicPaneCount = 1;
    private readonly int[] _mosaicSizes = { 1, 2, 4, 9, 16 };

    public int PaneCount => _mosaicPaneCount;
    public bool IsMosaicMode => _mosaicPaneCount > 1;

        public event Action<int>? PaneCountChanged; // new pane count
        public event Action<string>? LogMessage;

        public void IncreasePanes()
        {
            var currentSizeIndex = Array.IndexOf(_mosaicSizes, _mosaicPaneCount);
            var nextIndex = (currentSizeIndex + 1) % _mosaicSizes.Length;
            _mosaicPaneCount = _mosaicSizes[nextIndex];
            
            PaneCountChanged?.Invoke(_mosaicPaneCount);
            var plural = _mosaicPaneCount > 1 ? "s" : "";
            LogMessage?.Invoke(string.Format(Strings.Log_MosaicMode, _mosaicPaneCount, plural));
        }

        public void DecreasePanes()
        {
            var currentSizeIndex = Array.IndexOf(_mosaicSizes, _mosaicPaneCount);
            var prevIndex = (currentSizeIndex - 1 + _mosaicSizes.Length) % _mosaicSizes.Length;
            _mosaicPaneCount = _mosaicSizes[prevIndex];
            
            PaneCountChanged?.Invoke(_mosaicPaneCount);
            var plural = _mosaicPaneCount > 1 ? "s" : "";
            LogMessage?.Invoke(string.Format(Strings.Log_MosaicMode, _mosaicPaneCount, plural));
        }

        public void UpdateGridLayout(UniformGrid grid, double windowWidth, double windowHeight)
        {
            if (grid == null) return;

            // Handle special case for 2 panes - layout based on orientation
            if (_mosaicPaneCount == 2)
            {
                var isLandscape = windowWidth >= windowHeight;
                
                if (isLandscape)
                {
                    // Landscape: side by side (1 row, 2 columns)
                    grid.Rows = 1;
                    grid.Columns = 2;
                }
                else
                {
                    // Portrait: one under another (2 rows, 1 column)
                    grid.Rows = 2;
                    grid.Columns = 1;
                }
            }
            else
            {
                var gridSize = (int)Math.Sqrt(_mosaicPaneCount);
                grid.Rows = gridSize;
                grid.Columns = gridSize;
            }
        }

        public void ResetPaneIndex()
        {
            // No-op for now - kept for future use
        }
    }
