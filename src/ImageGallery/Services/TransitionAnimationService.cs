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
        
        private const int SLIDE_DURATION_MS = 400; // Duration of slide animation

        public TransitionAnimationService(ILogger<TransitionAnimationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Applies a slide-left transition animation to the ItemsControl displaying images.
        /// This is used for automatic navigation (slideshow) to provide visual feedback.
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
                _logger.LogTrace("Starting slide-left animation");

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
                
                // Create animation: slide from 0 to -width (left), then snap to +width, then slide to 0
                var storyboard = new Storyboard();
                
                // Phase 1: Slide current image out to the left
                var slideOutAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = -width,
                    Duration = TimeSpan.FromMilliseconds(SLIDE_DURATION_MS / 2),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                
                Storyboard.SetTarget(slideOutAnimation, translateTransform);
                Storyboard.SetTargetProperty(slideOutAnimation, new PropertyPath(TranslateTransform.XProperty));
                storyboard.Children.Add(slideOutAnimation);

                // Start the animation
                var tcs = new TaskCompletionSource<bool>();
                
                storyboard.Completed += (s, e) =>
                {
                    // After sliding out, position the new image off-screen to the right
                    translateTransform.X = width;
                    
                    // Phase 2: Slide new image in from the right
                    var slideInAnimation = new DoubleAnimation
                    {
                        From = width,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(SLIDE_DURATION_MS / 2),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    
                    var slideInStoryboard = new Storyboard();
                    Storyboard.SetTarget(slideInAnimation, translateTransform);
                    Storyboard.SetTargetProperty(slideInAnimation, new PropertyPath(TranslateTransform.XProperty));
                    slideInStoryboard.Children.Add(slideInAnimation);
                    
                    slideInStoryboard.Completed += (s2, e2) =>
                    {
                        _logger.LogTrace("Slide-left animation completed");
                        _isAnimating = false;
                        tcs.SetResult(true);
                    };
                    
                    slideInStoryboard.Begin();
                };

                storyboard.Begin();
                await tcs.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during slide animation");
                _isAnimating = false;
            }
        }

        /// <summary>
        /// Checks if an animation is currently in progress.
        /// </summary>
        public bool IsAnimating => _isAnimating;
    }
}
