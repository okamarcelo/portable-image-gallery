using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ImageGallery.Services
{
    /// <summary>
    /// Manages pause/play state and visual indicators.
    /// Single Responsibility: Handle pause state and icon animations.
    /// </summary>
    public class PauseController
    {
        private bool isPaused = false;
        private readonly DispatcherTimer blinkTimer;

        private FrameworkElement? pausePlayIcon;
        private Rectangle? pauseBar1;
        private Rectangle? pauseBar2;
        private Polygon? playTriangle;

        public bool IsPaused => isPaused;

        public event Action? Paused;
        public event Action? Resumed;
        public event Action<string>? LogMessage;

        public PauseController()
        {
            blinkTimer = new DispatcherTimer();
            blinkTimer.Interval = TimeSpan.FromMilliseconds(800);
            blinkTimer.Tick += BlinkTimer_Tick;
        }

        public void Initialize(FrameworkElement icon, Rectangle bar1, Rectangle bar2, Polygon triangle)
        {
            pausePlayIcon = icon;
            pauseBar1 = bar1;
            pauseBar2 = bar2;
            playTriangle = triangle;
        }

        public void Toggle()
        {
            isPaused = !isPaused;

            if (isPaused)
            {
                ShowPauseIcon();
                Paused?.Invoke();
                LogMessage?.Invoke("Paused");
            }
            else
            {
                ShowPlayIcon();
                Resumed?.Invoke();
                LogMessage?.Invoke("Resumed");
            }
        }

        public void Pause()
        {
            if (!isPaused)
            {
                Toggle();
            }
        }

        public void Resume()
        {
            if (isPaused)
            {
                Toggle();
            }
        }

        private void ShowPauseIcon()
        {
            if (pausePlayIcon == null) return;

            pausePlayIcon.Visibility = Visibility.Visible;
            if (pauseBar1 != null) pauseBar1.Visibility = Visibility.Visible;
            if (pauseBar2 != null) pauseBar2.Visibility = Visibility.Visible;
            if (playTriangle != null) playTriangle.Visibility = Visibility.Collapsed;

            blinkTimer.Start();
        }

        private void ShowPlayIcon()
        {
            if (pausePlayIcon == null) return;

            if (pauseBar1 != null) pauseBar1.Visibility = Visibility.Collapsed;
            if (pauseBar2 != null) pauseBar2.Visibility = Visibility.Collapsed;
            if (playTriangle != null) playTriangle.Visibility = Visibility.Visible;

            blinkTimer.Start();

            // Auto-hide after 2 seconds
            Task.Delay(2000).ContinueWith(_ =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (pausePlayIcon != null)
                    {
                        pausePlayIcon.Visibility = Visibility.Collapsed;
                    }
                    blinkTimer.Stop();
                });
            });
        }

        private void BlinkTimer_Tick(object? sender, EventArgs e)
        {
            if (isPaused)
            {
                if (pauseBar1 != null)
                    pauseBar1.Opacity = pauseBar1.Opacity == 0.6 ? 0.3 : 0.6;
                if (pauseBar2 != null)
                    pauseBar2.Opacity = pauseBar2.Opacity == 0.6 ? 0.3 : 0.6;
            }
            else
            {
                if (playTriangle != null)
                    playTriangle.Opacity = playTriangle.Opacity == 0.6 ? 0.3 : 0.6;
            }
        }
    }
}
