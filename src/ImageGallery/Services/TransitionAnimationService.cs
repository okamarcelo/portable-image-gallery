using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ImageGallery.Controls;

namespace ImageGallery.Services
{
    /// <summary>
    /// Handles slide transition animations for automatic navigation (slideshow mode).
    /// Single Responsibility: Manage visual transitions during automatic image changes.
    /// </summary>
    public class TransitionAnimationService
    {
        private readonly ILogger<TransitionAnimationService> _logger;

        public TransitionAnimationService(ILogger<TransitionAnimationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Enables slide transition on the SlidingItemsControl for the next item change only.
        /// The control itself handles the animation when items change, then automatically
        /// disables the transition to prevent it from running on manual navigation.
        /// </summary>
        /// <param name="slidingItemsControl">The SlidingItemsControl to enable transitions on</param>
        public async void EnableTransitionOnce(SlidingItemsControl slidingItemsControl)
        {
            if (slidingItemsControl == null)
            {
                _logger.LogWarning("SlidingItemsControl is null, cannot enable transition");
                return;
            }

            _logger.LogTrace("Enabling slide transition for next item change");
            slidingItemsControl.EnableTransition = true;
            
            // Wait a bit for the animation to start, then disable for next time
            await Task.Delay(100);
            while (slidingItemsControl.IsTransitioning)
            {
                await Task.Delay(50);
            }
            
            _logger.LogTrace("Transition completed, disabling for manual navigation");
            slidingItemsControl.EnableTransition = false;
        }

        /// <summary>
        /// Checks if an animation is currently in progress.
        /// </summary>
        public bool IsAnimating(SlidingItemsControl slidingItemsControl)
        {
            return slidingItemsControl?.IsTransitioning ?? false;
        }
    }
}
