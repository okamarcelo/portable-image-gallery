using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ImageGallery.Services
{
    /// <summary>
    /// Service that manages slide transitions for an ItemsControl by temporarily duplicating
    /// content and animating both old and new content simultaneously.
    /// </summary>
    public class SlideTransitionService
    {
        private bool _isTransitioning = false;
        private readonly TimeSpan _transitionDuration = TimeSpan.FromMilliseconds(400);

        /// <summary>
        /// Performs a slide transition animation on the given ItemsControl.
        /// The old content slides out to the left while new content slides in from the right.
        /// </summary>
        public async Task AnimateTransitionAsync(ItemsControl itemsControl, IEnumerable newItems)
        {
            if (_isTransitioning || itemsControl == null || newItems == null)
                return;

            _isTransitioning = true;

            try
            {
                // Get the current items before changing
                var oldItems = itemsControl.ItemsSource?.Cast<object>().ToList();
                
                if (oldItems == null || !oldItems.Any())
                {
                    // No transition needed for first item
                    itemsControl.ItemsSource = newItems;
                    _isTransitioning = false;
                    return;
                }

                // Create a Grid to hold both ItemsControls during transition
                var parent = itemsControl.Parent as Panel;
                if (parent == null)
                {
                    itemsControl.ItemsSource = newItems;
                    _isTransitioning = false;
                    return;
                }

                var index = parent.Children.IndexOf(itemsControl);
                
                // Create a temporary grid for the transition
                var transitionGrid = new Grid { ClipToBounds = true };
                
                // Create the "old" ItemsControl (will slide left)
                var oldItemsControl = CloneItemsControl(itemsControl);
                oldItemsControl.ItemsSource = oldItems;
                
                // Create the "new" ItemsControl (will slide in from right)
                var newItemsControl = CloneItemsControl(itemsControl);
                newItemsControl.ItemsSource = newItems;
                
                // Position new control off-screen to the right
                var newTransform = new TranslateTransform { X = itemsControl.ActualWidth };
                newItemsControl.RenderTransform = newTransform;
                
                var oldTransform = new TranslateTransform { X = 0 };
                oldItemsControl.RenderTransform = oldTransform;
                
                transitionGrid.Children.Add(oldItemsControl);
                transitionGrid.Children.Add(newItemsControl);
                
                // Replace the original control with our transition grid
                parent.Children.RemoveAt(index);
                parent.Children.Insert(index, transitionGrid);
                
                // Animate both controls
                var oldAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = -itemsControl.ActualWidth,
                    Duration = _transitionDuration,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                
                var newAnimation = new DoubleAnimation
                {
                    From = itemsControl.ActualWidth,
                    To = 0,
                    Duration = _transitionDuration,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                
                oldTransform.BeginAnimation(TranslateTransform.XProperty, oldAnimation);
                newTransform.BeginAnimation(TranslateTransform.XProperty, newAnimation);
                
                // Wait for animation to complete
                await Task.Delay(_transitionDuration);
                
                // Replace the transition grid with the original control showing new items
                parent.Children.RemoveAt(index);
                itemsControl.ItemsSource = newItems;
                parent.Children.Insert(index, itemsControl);
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private ItemsControl CloneItemsControl(ItemsControl source)
        {
            var clone = new ItemsControl
            {
                ItemTemplate = source.ItemTemplate,
                ItemsPanel = source.ItemsPanel,
                HorizontalAlignment = source.HorizontalAlignment,
                VerticalAlignment = source.VerticalAlignment,
                Width = source.ActualWidth,
                Height = source.ActualHeight
            };
            
            return clone;
        }
    }
}
