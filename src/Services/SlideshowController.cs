using System;
using System.Windows.Threading;

namespace ImageGallery.Services
{
    /// <summary>
    /// Manages slideshow timer and auto-advance functionality.
    /// Single Responsibility: Handle slideshow timing and progression.
    /// </summary>
    public class SlideshowController
    {
        private readonly DispatcherTimer timer;
        private double intervalSeconds = 5.0;

        public double IntervalSeconds => intervalSeconds;
        public bool IsRunning => timer.IsEnabled;

        public event Action? Tick; // fired on each timer tick
        public event Action<double>? IntervalChanged; // new interval
        public event Action<string>? LogMessage;

        public SlideshowController()
        {
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            UpdateInterval();
        }

        public void Start()
        {
            if (!timer.IsEnabled)
            {
                timer.Start();
                LogMessage?.Invoke("Slideshow started");
            }
        }

        public void Stop()
        {
            if (timer.IsEnabled)
            {
                timer.Stop();
                LogMessage?.Invoke("Slideshow stopped");
            }
        }

        public void Restart()
        {
            timer.Stop();
            timer.Start();
        }

        public void IncreaseSpeed()
        {
            intervalSeconds += 0.5;
            UpdateInterval();
            IntervalChanged?.Invoke(intervalSeconds);
            LogMessage?.Invoke($"Slideshow speed: {intervalSeconds:0.0}s");
        }

        public void DecreaseSpeed()
        {
            if (intervalSeconds > 0.5)
            {
                intervalSeconds -= 0.5;
                UpdateInterval();
                IntervalChanged?.Invoke(intervalSeconds);
                LogMessage?.Invoke($"Slideshow speed: {intervalSeconds:0.0}s");
            }
        }

        private void UpdateInterval()
        {
            timer.Interval = TimeSpan.FromSeconds(intervalSeconds);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            Tick?.Invoke();
        }
    }
}
