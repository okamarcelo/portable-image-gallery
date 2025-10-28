using System;
using System.Windows.Threading;
using ImageGallery.Resources;

namespace ImageGallery.Services;

/// <summary>
/// Manages slideshow timer and auto-advance functionality.
/// Single Responsibility: Handle slideshow timing and progression.
/// </summary>
public class SlideshowController
    {
        private readonly DispatcherTimer _timer;
        private double _intervalSeconds = 5.0;

        public double IntervalSeconds => _intervalSeconds;
        public bool IsRunning => _timer.IsEnabled;

        public event Action? Tick; // fired on each timer tick
        public event Action<double>? IntervalChanged; // new interval
        public event Action<string>? LogMessage;

        public SlideshowController()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            UpdateInterval();
        }

        public void Start()
        {
            if (!_timer.IsEnabled)
            {
                _timer.Start();
                LogMessage?.Invoke(Strings.Status_SlideshowStarted);
            }
        }

        public void Stop()
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
                LogMessage?.Invoke(Strings.Status_SlideshowStopped);
            }
        }

        public void Restart()
        {
            _timer.Stop();
            _timer.Start();
        }

        public void IncreaseSpeed()
        {
            _intervalSeconds += 0.5;
            UpdateInterval();
            IntervalChanged?.Invoke(_intervalSeconds);
            LogMessage?.Invoke(string.Format(Strings.Log_SlideshowSpeed, _intervalSeconds));
        }

        public void DecreaseSpeed()
        {
            if (_intervalSeconds > 0.5)
            {
                _intervalSeconds -= 0.5;
                UpdateInterval();
                IntervalChanged?.Invoke(_intervalSeconds);
                LogMessage?.Invoke(string.Format(Strings.Log_SlideshowSpeed, _intervalSeconds));
            }
        }

        private void UpdateInterval()
        {
            _timer.Interval = TimeSpan.FromSeconds(_intervalSeconds);
        }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        Tick?.Invoke();
    }
}