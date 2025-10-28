using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ImageGallery.Resources;

namespace ImageGallery.Services;

/// <summary>
/// Manages zoom and pan functionality for image viewing.
/// Single Responsibility: Handle zoom level and image panning.
/// </summary>
public class ZoomController
    {
        private double _zoomLevel = 1.0;
        private Point _panOffset = new Point(0, 0);
        private Point _lastMousePosition;
        private bool _isDraggingImage = false;

        private ScaleTransform? _scaleTransform;
        private TranslateTransform? _translateTransform;

        public double ZoomLevel => _zoomLevel;
        public bool IsZoomed => _zoomLevel > 1.0;
        public int ZoomPercent => (int)(_zoomLevel * 100);

        public event Action<double>? ZoomChanged; // zoom level
        public event Action? ZoomedIn; // fired when zoom > 1.0
        public event Action? ZoomedOut; // fired when zoom returns to 1.0
        public event Action<string>? LogMessage;

        public void Initialize(ScaleTransform scale, TranslateTransform translate)
        {
            _scaleTransform = scale;
            _translateTransform = translate;
        }

        public void ZoomIn()
        {
            var wasNotZoomed = _zoomLevel <= 1.0;
            
            _zoomLevel += 0.1;
            ApplyZoom();
            
            if (wasNotZoomed && _zoomLevel > 1.0)
            {
                ZoomedIn?.Invoke();
            }
        }

        public void ZoomOut()
        {
            if (_zoomLevel > 1.0)
            {
                _zoomLevel -= 0.1;
                if (_zoomLevel < 1.0) _zoomLevel = 1.0;
                ApplyZoom();
                
                if (_zoomLevel <= 1.0)
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
                _isDraggingImage = true;
                _lastMousePosition = mousePosition;
            }
        }

        public void EndDrag()
        {
            _isDraggingImage = false;
        }

        public void UpdateDrag(Point currentPosition)
        {
            if (_isDraggingImage)
            {
                var delta = currentPosition - _lastMousePosition;
                
                _panOffset.X += delta.X;
                _panOffset.Y += delta.Y;
                
                ApplyPan();
                
                _lastMousePosition = currentPosition;
            }
        }

        public void ResetZoom()
        {
            _zoomLevel = 1.0;
            ResetPan();
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (_scaleTransform != null)
            {
                _scaleTransform.ScaleX = _zoomLevel;
                _scaleTransform.ScaleY = _zoomLevel;
            }
            
            ZoomChanged?.Invoke(_zoomLevel);
            LogMessage?.Invoke(string.Format(Strings.Log_Zoom, ZoomPercent));
        }

        private void ApplyPan()
        {
            if (_translateTransform != null)
            {
                _translateTransform.X = _panOffset.X;
                _translateTransform.Y = _panOffset.Y;
            }
        }

    private void ResetPan()
    {
        _panOffset = new Point(0, 0);
        ApplyPan();
    }
}