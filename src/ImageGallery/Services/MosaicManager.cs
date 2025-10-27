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
    private int mosaicPaneCount = 1;
    private readonly int[] mosaicSizes = { 1, 2, 4, 9, 16 };

    public int PaneCount => mosaicPaneCount;
    public bool IsMosaicMode => mosaicPaneCount > 1;

        public event Action<int>? PaneCountChanged; // new pane count
        public event Action<string>? LogMessage;

        public void IncreasePanes()
        {
            int currentSizeIndex = Array.IndexOf(mosaicSizes, mosaicPaneCount);
            int nextIndex = (currentSizeIndex + 1) % mosaicSizes.Length;
            mosaicPaneCount = mosaicSizes[nextIndex];
            
            PaneCountChanged?.Invoke(mosaicPaneCount);
            string plural = mosaicPaneCount > 1 ? "s" : "";
            LogMessage?.Invoke(string.Format(Strings.Log_MosaicMode, mosaicPaneCount, plural));
        }

        public void DecreasePanes()
        {
            int currentSizeIndex = Array.IndexOf(mosaicSizes, mosaicPaneCount);
            int prevIndex = (currentSizeIndex - 1 + mosaicSizes.Length) % mosaicSizes.Length;
            mosaicPaneCount = mosaicSizes[prevIndex];
            
            PaneCountChanged?.Invoke(mosaicPaneCount);
            string plural = mosaicPaneCount > 1 ? "s" : "";
            LogMessage?.Invoke(string.Format(Strings.Log_MosaicMode, mosaicPaneCount, plural));
        }

        public void UpdateGridLayout(UniformGrid grid, double windowWidth, double windowHeight)
        {
            if (grid == null) return;

            // Handle special case for 2 panes - layout based on orientation
            if (mosaicPaneCount == 2)
            {
                bool isLandscape = windowWidth >= windowHeight;
                
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
                int gridSize = (int)Math.Sqrt(mosaicPaneCount);
                grid.Rows = gridSize;
                grid.Columns = gridSize;
            }
        }

        public void ResetPaneIndex()
        {
            // No-op for now - kept for future use
        }
    }
