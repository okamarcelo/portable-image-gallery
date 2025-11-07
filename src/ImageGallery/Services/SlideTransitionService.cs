using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ImageGallery.Services
{
    /// <summary>
    /// Service that manages slide transitions for an ItemsControl by temporarily duplicating
    /// content and animating both old and new content simultaneously.
    /// </summary>
    public class SlideTransitionService
    {
        /// <summary>
        /// Duration of the slide transition animation in milliseconds
        /// </summary>
        private const int TransitionDurationMs = 400;
        
        private bool _isTransitioning = false;
        private bool _transitionEnabled = false;
        private readonly TimeSpan _transitionDuration = TimeSpan.FromMilliseconds(TransitionDurationMs);
        private ItemsControl? _trackedControl;
        private IEnumerable? _pendingItems;

        /// <summary>
        /// Enable the transition for the next item change
        /// </summary>
        public void EnableTransitionOnce()
        {
            _transitionEnabled = true;
        }

        /// <summary>
        /// Registers an ItemsControl to be tracked for transitions
        /// </summary>
        public void RegisterControl(ItemsControl control)
        {
            _trackedControl = control;
        }

        /// <summary>
        /// Updates the ItemsControl with new items, applying transition if enabled.
        /// </summary>
        public async Task UpdateItemsAsync(IEnumerable? newItems)
        {
            if (_trackedControl == null || newItems == null)
            {
                if (_trackedControl != null)
                    _trackedControl.ItemsSource = newItems;
                return;
            }

            // Materialize collection once to avoid multiple enumerations
            var itemsList = newItems.Cast<object>().ToList();
            var shouldTransition = _transitionEnabled && !_isTransitioning && itemsList.Count == 1;

            if (!shouldTransition)
            {
                // No transition - just update directly
                _trackedControl.ItemsSource = itemsList;
                _transitionEnabled = false; // Reset flag
                return;
            }

            // Perform transition
            await AnimateTransitionAsync(itemsList);
        }

        private async Task AnimateTransitionAsync(IEnumerable newItems)
        {
            if (_trackedControl == null || newItems == null)
                return;

            _isTransitioning = true;
            _transitionEnabled = false; // Auto-disable after use

            try
            {
                // Get the current items before changing
                var oldItems = _trackedControl.ItemsSource?.Cast<object>().ToList();
                
                if (oldItems == null || !oldItems.Any())
                {
                    // No transition needed for first item
                    _trackedControl.ItemsSource = newItems;
                    return;
                }

                // Get parent grid
                var parent = _trackedControl.Parent as Grid;
                if (parent == null)
                {
                    _trackedControl.ItemsSource = newItems;
                    return;
                }

                var index = parent.Children.IndexOf(_trackedControl);
                var width = _trackedControl.ActualWidth;
                
                if (width <= 0)
                {
                    _trackedControl.ItemsSource = newItems;
                    return;
                }

                // Store original properties
                var originalName = _trackedControl.Name;
                var originalTemplate = _trackedControl.ItemTemplate;
                var originalPanel = _trackedControl.ItemsPanel;
                
                // Create a temporary grid for the transition
                var transitionGrid = new Grid 
                { 
                    ClipToBounds = true,
                    Background = System.Windows.Media.Brushes.Transparent
                };
                
                // Create the "old" ItemsControl (will slide left)
                var oldItemsControl = new ItemsControl
                {
                    ItemTemplate = originalTemplate,
                    ItemsPanel = originalPanel,
                    ItemsSource = oldItems,
                    RenderTransform = new TranslateTransform { X = 0 }
                };
                
                // Create the "new" ItemsControl (will slide in from right)
                var newItemsControl = new ItemsControl
                {
                    ItemTemplate = originalTemplate,
                    ItemsPanel = originalPanel,
                    ItemsSource = newItems,
                    RenderTransform = new TranslateTransform { X = width }
                };
                
                transitionGrid.Children.Add(oldItemsControl);
                transitionGrid.Children.Add(newItemsControl);
                
                // Replace the original control with our transition grid
                parent.Children.RemoveAt(index);
                parent.Children.Insert(index, transitionGrid);
                
                // Force layout update
                transitionGrid.UpdateLayout();
                
                // Get the transforms
                var oldTransform = (TranslateTransform)oldItemsControl.RenderTransform;
                var newTransform = (TranslateTransform)newItemsControl.RenderTransform;
                
                // Create animations
                var oldAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = -width,
                    Duration = _transitionDuration,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                
                var newAnimation = new DoubleAnimation
                {
                    From = width,
                    To = 0,
                    Duration = _transitionDuration,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                
                // Start animations
                oldTransform.BeginAnimation(TranslateTransform.XProperty, oldAnimation);
                newTransform.BeginAnimation(TranslateTransform.XProperty, newAnimation);
                
                // Wait for animation to complete
                await Task.Delay(_transitionDuration);
                
                // Replace the transition grid with the original control showing new items
                parent.Children.RemoveAt(index);
                _trackedControl.ItemsSource = newItems;
                parent.Children.Insert(index, _trackedControl);
            }
            finally
            {
                _isTransitioning = false;
            }
        }
    }
}
