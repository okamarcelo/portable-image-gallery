using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Threading;
using ImageGallery.Resources;

namespace ImageGallery.Services;

/// <summary>
/// Manages pause/play state and visual indicators.
/// Single Responsibility: Handle pause state and icon animations.
/// </summary>
public class PauseController
    {
        private bool _isPaused = false;
        private readonly DispatcherTimer _blinkTimer;

        private FrameworkElement? _pausePlayIcon;
        private Rectangle? _pauseBar1;
        private Rectangle? _pauseBar2;
        private Polygon? _playTriangle;

        public bool IsPaused => _isPaused;

        public event Action? Paused;
        public event Action? Resumed;
        public event Action<string>? LogMessage;

        public PauseController()
        {
            _blinkTimer = new DispatcherTimer();
            _blinkTimer.Interval = TimeSpan.FromMilliseconds(800);
            _blinkTimer.Tick += BlinkTimer_Tick;
        }

        public void Initialize(FrameworkElement icon, Rectangle bar1, Rectangle bar2, Polygon triangle)
        {
            _pausePlayIcon = icon;
            _pauseBar1 = bar1;
            _pauseBar2 = bar2;
            _playTriangle = triangle;
        }

        public void Toggle()
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                ShowPauseIcon();
                Paused?.Invoke();
                LogMessage?.Invoke(Strings.Status_Paused);
            }
            else
            {
                ShowPlayIcon();
                Resumed?.Invoke();
                LogMessage?.Invoke(Strings.Status_Resumed);
            }
        }

        public void Pause()
        {
            if (!_isPaused)
            {
                Toggle();
            }
        }

        public void Resume()
        {
            if (_isPaused)
            {
                Toggle();
            }
        }

        private void ShowPauseIcon()
        {
            if (_pausePlayIcon == null) return;

            _pausePlayIcon.Visibility = Visibility.Visible;
            if (_pauseBar1 != null) _pauseBar1.Visibility = Visibility.Visible;
            if (_pauseBar2 != null) _pauseBar2.Visibility = Visibility.Visible;
            if (_playTriangle != null) _playTriangle.Visibility = Visibility.Collapsed;

            _blinkTimer.Start();
        }

        private void ShowPlayIcon()
        {
            if (_pausePlayIcon == null) return;

            if (_pauseBar1 != null) _pauseBar1.Visibility = Visibility.Collapsed;
            if (_pauseBar2 != null) _pauseBar2.Visibility = Visibility.Collapsed;
            if (_playTriangle != null) _playTriangle.Visibility = Visibility.Visible;

            _blinkTimer.Start();

            // Auto-hide after 2 seconds
            Task.Delay(2000).ContinueWith(_ =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (_pausePlayIcon != null)
                    {
                        _pausePlayIcon.Visibility = Visibility.Collapsed;
                    }
                    _blinkTimer.Stop();
                });
            });
        }

        private void BlinkTimer_Tick(object? sender, EventArgs e)
        {
            if (_isPaused)
            {
                if (_pauseBar1 != null)
                    _pauseBar1.Opacity = _pauseBar1.Opacity == 0.6 ? 0.3 : 0.6;
            if (_pauseBar2 != null)
                _pauseBar2.Opacity = _pauseBar2.Opacity == 0.6 ? 0.3 : 0.6;
        }
        else
        {
            if (_playTriangle != null)
                _playTriangle.Opacity = _playTriangle.Opacity == 0.6 ? 0.3 : 0.6;
        }
    }
}