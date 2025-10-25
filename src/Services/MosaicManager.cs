using System;
using System.Windows.Controls.Primitives;

namespace ImageGallery.Services
{
    /// <summary>
    /// Manages mosaic display configuration and layout.
    /// Single Responsibility: Handle mosaic pane count and grid layout.
    /// </summary>
    public class MosaicManager
    {
        private int mosaicPaneCount = 1;
        private readonly int[] mosaicSizes = { 1, 2, 4, 9, 16 };
        private int currentMosaicPaneIndex = 0;

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
            LogMessage?.Invoke($"Mosaic mode: {mosaicPaneCount} pane{(mosaicPaneCount > 1 ? "s" : "")}");
        }

        public void DecreasePanes()
        {
            int currentSizeIndex = Array.IndexOf(mosaicSizes, mosaicPaneCount);
            int prevIndex = (currentSizeIndex - 1 + mosaicSizes.Length) % mosaicSizes.Length;
            mosaicPaneCount = mosaicSizes[prevIndex];
            
            PaneCountChanged?.Invoke(mosaicPaneCount);
            LogMessage?.Invoke($"Mosaic mode: {mosaicPaneCount} pane{(mosaicPaneCount > 1 ? "s" : "")}");
        }

        public void UpdateGridLayout(UniformGrid grid)
        {
            if (grid == null) return;

            // Handle special case for 2 panes (1x2 layout)
            if (mosaicPaneCount == 2)
            {
                grid.Rows = 1;
                grid.Columns = 2;
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
            currentMosaicPaneIndex = 0;
        }
    }
}
