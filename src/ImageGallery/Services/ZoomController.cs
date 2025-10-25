using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ImageGallery.Services;

/// <summary>
/// Manages zoom and pan functionality for image viewing.
/// Single Responsibility: Handle zoom level and image panning.
/// </summary>
public class ZoomController
    {
        private double zoomLevel = 1.0;
        private Point panOffset = new Point(0, 0);
        private Point lastMousePosition;
        private bool isDraggingImage = false;

        private ScaleTransform? scaleTransform;
        private TranslateTransform? translateTransform;

        public double ZoomLevel => zoomLevel;
        public bool IsZoomed => zoomLevel > 1.0;
        public int ZoomPercent => (int)(zoomLevel * 100);

        public event Action<double>? ZoomChanged; // zoom level
        public event Action? ZoomedIn; // fired when zoom > 1.0
        public event Action? ZoomedOut; // fired when zoom returns to 1.0
        public event Action<string>? LogMessage;

        public void Initialize(ScaleTransform scale, TranslateTransform translate)
        {
            scaleTransform = scale;
            translateTransform = translate;
        }

        public void ZoomIn()
        {
            bool wasNotZoomed = zoomLevel <= 1.0;
            
            zoomLevel += 0.1;
            ApplyZoom();
            
            if (wasNotZoomed && zoomLevel > 1.0)
            {
                ZoomedIn?.Invoke();
            }
        }

        public void ZoomOut()
        {
            if (zoomLevel > 1.0)
            {
                zoomLevel -= 0.1;
                if (zoomLevel < 1.0) zoomLevel = 1.0;
                ApplyZoom();
                
                if (zoomLevel <= 1.0)
                {
                    ResetPan();
                    ZoomedOut?.Invoke();
                }
            }
        }

        public void HandleMouseWheel(MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                ZoomIn();
            }
            else
            {
                ZoomOut();
            }
        }

        public void StartDrag(Point mousePosition)
        {
            if (IsZoomed)
            {
                isDraggingImage = true;
                lastMousePosition = mousePosition;
            }
        }

        public void EndDrag()
        {
            isDraggingImage = false;
        }

        public void UpdateDrag(Point currentPosition)
        {
            if (isDraggingImage)
            {
                Vector delta = currentPosition - lastMousePosition;
                
                panOffset.X += delta.X;
                panOffset.Y += delta.Y;
                
                ApplyPan();
                
                lastMousePosition = currentPosition;
            }
        }

        public void ResetZoom()
        {
            zoomLevel = 1.0;
            ResetPan();
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (scaleTransform != null)
            {
                scaleTransform.ScaleX = zoomLevel;
                scaleTransform.ScaleY = zoomLevel;
            }
            
            ZoomChanged?.Invoke(zoomLevel);
            LogMessage?.Invoke($"Zoom: {ZoomPercent}%");
        }

        private void ApplyPan()
        {
            if (translateTransform != null)
            {
                translateTransform.X = panOffset.X;
                translateTransform.Y = panOffset.Y;
            }
        }

    private void ResetPan()
    {
        panOffset = new Point(0, 0);
        ApplyPan();
    }
}