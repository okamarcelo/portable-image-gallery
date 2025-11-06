using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ImageGallery.Services
{
    /// <summary>
    /// Handles slide transition animations for automatic navigation (slideshow mode).
    /// Single Responsibility: Manage visual transitions during automatic image changes.
    /// </summary>
    public class TransitionAnimationService
    {
        private readonly ILogger<TransitionAnimationService> _logger;
        private bool _isAnimating = false;
        
        private const int TRANSITION_DURATION_MS = 300; // Duration of transition animation

        public TransitionAnimationService(ILogger<TransitionAnimationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Applies a slide-left transition animation to the ItemsControl displaying images.
        /// This is used for automatic navigation (slideshow) to provide visual feedback.
        /// Uses a translate transform to slide the entire control left and back.
        /// </summary>
        /// <param name="itemsControl">The ItemsControl containing the images</param>
        /// <returns>Task that completes when animation finishes</returns>
        public async Task AnimateSlideLeftAsync(ItemsControl itemsControl)
        {
            if (_isAnimating || itemsControl == null)
                return;

            _isAnimating = true;
            
            try
            {
                _logger.LogTrace("Starting slide-left transition");

                // Get the translate transform (should already exist from zoom functionality)
                var transformGroup = itemsControl.RenderTransform as TransformGroup;
                if (transformGroup == null || transformGroup.Children.Count < 2)
                {
                    _logger.LogWarning("TransformGroup not found or incomplete, skipping animation");
                    _isAnimating = false;
                    return;
                }

                var translateTransform = transformGroup.Children[1] as TranslateTransform;
                if (translateTransform == null)
                {
                    _logger.LogWarning("TranslateTransform not found, skipping animation");
                    _isAnimating = false;
                    return;
                }

                // Get the actual width of the control for animation distance
                var width = itemsControl.ActualWidth;
                
                if (width <= 0)
                {
                    _logger.LogWarning("Control width is 0, skipping animation");
                    _isAnimating = false;
                    return;
                }
                
                // Save current X position (likely 0 unless zoomed/panned)
                var originalX = translateTransform.X;
                
                // Create animation: slide from right to center (gives appearance of sliding left)
                // Start from right side, end at original position
                var slideAnimation = new DoubleAnimation
                {
                    From = originalX + width * 0.3, // Start 30% to the right
                    To = originalX, // End at original position
                    Duration = TimeSpan.FromMilliseconds(TRANSITION_DURATION_MS),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                
                var storyboard = new Storyboard();
                Storyboard.SetTarget(slideAnimation, translateTransform);
                Storyboard.SetTargetProperty(slideAnimation, new PropertyPath(TranslateTransform.XProperty));
                storyboard.Children.Add(slideAnimation);

                // Create opacity fade-in for smooth appearance
                var opacityAnimation = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(TRANSITION_DURATION_MS * 0.6)
                };
                
                Storyboard.SetTarget(opacityAnimation, itemsControl);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(opacityAnimation);

                // Start the animation
                var tcs = new TaskCompletionSource<bool>();
                
                storyboard.Completed += (s, e) =>
                {
                    _logger.LogTrace("Slide-left transition completed");
                    _isAnimating = false;
                    tcs.SetResult(true);
                };

                storyboard.Begin();
                await tcs.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during slide transition");
                _isAnimating = false;
            }
        }

        /// <summary>
        /// Checks if an animation is currently in progress.
        /// </summary>
        public bool IsAnimating => _isAnimating;
    }
}
