using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ImageGallery.Controls
{
    /// <summary>
    /// An ItemsControl wrapper that provides sliding transition animations
    /// when the items change. Displays both old and new items simultaneously during the transition.
    /// </summary>
    public class SlidingItemsControl : Control
    {
        private Grid? _rootGrid;
        private ItemsControl? _currentItemsControl;
        private ItemsControl? _previousItemsControl;
        private bool _isTransitioning = false;
        private IEnumerable? _previousItems;

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(SlidingItemsControl),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(
                nameof(ItemTemplate),
                typeof(DataTemplate),
                typeof(SlidingItemsControl),
                new PropertyMetadata(null, OnItemTemplateChanged));

        public static readonly DependencyProperty ItemsPanelProperty =
            DependencyProperty.Register(
                nameof(ItemsPanel),
                typeof(ItemsPanelTemplate),
                typeof(SlidingItemsControl),
                new PropertyMetadata(null, OnItemsPanelChanged));

        public static readonly DependencyProperty TransitionDurationProperty =
            DependencyProperty.Register(
                nameof(TransitionDuration),
                typeof(TimeSpan),
                typeof(SlidingItemsControl),
                new PropertyMetadata(TimeSpan.FromMilliseconds(400)));

        public static readonly DependencyProperty EnableTransitionProperty =
            DependencyProperty.Register(
                nameof(EnableTransition),
                typeof(bool),
                typeof(SlidingItemsControl),
                new PropertyMetadata(false));

        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public DataTemplate? ItemTemplate
        {
            get => (DataTemplate?)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public ItemsPanelTemplate? ItemsPanel
        {
            get => (ItemsPanelTemplate?)GetValue(ItemsPanelProperty);
            set => SetValue(ItemsPanelProperty, value);
        }

        public TimeSpan TransitionDuration
        {
            get => (TimeSpan)GetValue(TransitionDurationProperty);
            set => SetValue(TransitionDurationProperty, value);
        }

        public bool EnableTransition
        {
            get => (bool)GetValue(EnableTransitionProperty);
            set => SetValue(EnableTransitionProperty, value);
        }

        public SlidingItemsControl()
        {
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[SlidingItemsControl] OnLoaded called");
            if (_rootGrid == null)
            {
                System.Diagnostics.Debug.WriteLine("[SlidingItemsControl] Building visual tree");
                BuildVisualTree();
            }
        }

        private void BuildVisualTree()
        {
            System.Diagnostics.Debug.WriteLine("[SlidingItemsControl] BuildVisualTree started");
            _rootGrid = new Grid
            {
                ClipToBounds = true,
                Background = System.Windows.Media.Brushes.Transparent
            };

            // Create the current ItemsControl
            _currentItemsControl = CreateItemsControl();
            System.Diagnostics.Debug.WriteLine($"[SlidingItemsControl] Created current ItemsControl, ItemsSource is null: {ItemsSource == null}");
            if (ItemsSource != null)
            {
                _currentItemsControl.ItemsSource = ItemsSource;
                System.Diagnostics.Debug.WriteLine($"[SlidingItemsControl] Set ItemsSource on current ItemsControl");
            }

            // Create the previous ItemsControl (initially hidden)
            _previousItemsControl = CreateItemsControl();
            _previousItemsControl.Visibility = Visibility.Collapsed;

            _rootGrid.Children.Add(_previousItemsControl);
            _rootGrid.Children.Add(_currentItemsControl);

            AddVisualChild(_rootGrid);
            AddLogicalChild(_rootGrid);
            System.Diagnostics.Debug.WriteLine("[SlidingItemsControl] BuildVisualTree completed");
        }

        private ItemsControl CreateItemsControl()
        {
            var itemsControl = new ItemsControl
            {
                RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
                    {
                        new ScaleTransform { ScaleX = 1, ScaleY = 1 },
                        new TranslateTransform { X = 0, Y = 0 }
                    }
                },
                RenderTransformOrigin = new Point(0.5, 0.5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Apply template and panel from properties
            if (ItemTemplate != null)
                itemsControl.ItemTemplate = ItemTemplate;
            
            if (ItemsPanel != null)
                itemsControl.ItemsPanel = ItemsPanel;

            return itemsControl;
        }

        protected override int VisualChildrenCount => _rootGrid != null ? 1 : 0;

        protected override Visual? GetVisualChild(int index)
        {
            if (index == 0 && _rootGrid != null)
                return _rootGrid;
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _rootGrid?.Measure(constraint);
            return _rootGrid?.DesiredSize ?? base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _rootGrid?.Arrange(new Rect(arrangeBounds));
            return arrangeBounds;
        }

        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SlidingItemsControl)d;
            var template = e.NewValue as DataTemplate;
            System.Diagnostics.Debug.WriteLine($"[SlidingItemsControl] OnItemTemplateChanged: template={(template == null ? "null" : "notNull")}");
            if (control._currentItemsControl != null && template != null)
                control._currentItemsControl.ItemTemplate = template;
            if (control._previousItemsControl != null && template != null)
                control._previousItemsControl.ItemTemplate = template;
        }

        private static void OnItemsPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SlidingItemsControl)d;
            var panel = e.NewValue as ItemsPanelTemplate;
            System.Diagnostics.Debug.WriteLine($"[SlidingItemsControl] OnItemsPanelChanged: panel={(panel == null ? "null" : "notNull")}");
            if (control._currentItemsControl != null && panel != null)
                control._currentItemsControl.ItemsPanel = panel;
            if (control._previousItemsControl != null && panel != null)
                control._previousItemsControl.ItemsPanel = panel;
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SlidingItemsControl)d;
            control.OnItemsSourceChanged(e.OldValue as IEnumerable, e.NewValue as IEnumerable);
        }

        private void OnItemsSourceChanged(IEnumerable? oldItems, IEnumerable? newItems)
        {
            System.Diagnostics.Debug.WriteLine($"[SlidingItemsControl] OnItemsSourceChanged: old={(oldItems == null ? "null" : "notNull")}, new={(newItems == null ? "null" : "notNull")}, EnableTransition={EnableTransition}, _isTransitioning={_isTransitioning}");
            
            if (_currentItemsControl == null || _previousItemsControl == null || _rootGrid == null)
            {
                System.Diagnostics.Debug.WriteLine("[SlidingItemsControl] Controls not initialized yet, saving items for later");
                _previousItems = newItems;
                return;
            }

            // If transitions are disabled or this is the first content, just update instantly
            if (!EnableTransition || oldItems == null || _isTransitioning)
            {
                System.Diagnostics.Debug.WriteLine("[SlidingItemsControl] Updating instantly (no transition)");
                _currentItemsControl.ItemsSource = newItems;
                _previousItems = newItems;
                return;
            }

            // Start the slide transition
            System.Diagnostics.Debug.WriteLine("[SlidingItemsControl] Starting slide transition");
            BeginSlideTransition(oldItems, newItems);
        }

        private async void BeginSlideTransition(IEnumerable? oldItems, IEnumerable? newItems)
        {
            if (_currentItemsControl == null || _previousItemsControl == null || _rootGrid == null)
                return;

            _isTransitioning = true;

            try
            {
                var width = ActualWidth;
                if (width <= 0)
                {
                    _currentItemsControl.ItemsSource = newItems;
                    _isTransitioning = false;
                    _previousItems = newItems;
                    return;
                }

                // Move old content to previous presenter
                _previousItemsControl.ItemsSource = oldItems;
                _previousItemsControl.Visibility = Visibility.Visible;

                // Set new content to current presenter, positioned off-screen to the right
                _currentItemsControl.ItemsSource = newItems;
                
                var currentTransform = (TranslateTransform)((TransformGroup)_currentItemsControl.RenderTransform).Children[1];
                currentTransform.X = width;

                var previousTransform = (TranslateTransform)((TransformGroup)_previousItemsControl.RenderTransform).Children[1];
                previousTransform.X = 0;

                // Create animations for both presenters
                var currentAnimation = new DoubleAnimation
                {
                    From = width,
                    To = 0,
                    Duration = TransitionDuration,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                var previousAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = -width,
                    Duration = TransitionDuration,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                var storyboard = new Storyboard();
                
                Storyboard.SetTarget(currentAnimation, currentTransform);
                Storyboard.SetTargetProperty(currentAnimation, new PropertyPath(TranslateTransform.XProperty));
                
                Storyboard.SetTarget(previousAnimation, previousTransform);
                Storyboard.SetTargetProperty(previousAnimation, new PropertyPath(TranslateTransform.XProperty));

                storyboard.Children.Add(currentAnimation);
                storyboard.Children.Add(previousAnimation);

                var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                storyboard.Completed += (s, e) =>
                {
                    _previousItemsControl.Visibility = Visibility.Collapsed;
                    _previousItemsControl.ItemsSource = null;
                    _isTransitioning = false;
                    _previousItems = newItems;
                    tcs.SetResult(true);
                };

                storyboard.Begin();
                await tcs.Task;
            }
            catch
            {
                _currentItemsControl.ItemsSource = newItems;
                _previousItemsControl.Visibility = Visibility.Collapsed;
                _previousItemsControl.ItemsSource = null;
                _isTransitioning = false;
                _previousItems = newItems;
            }
        }

        public bool IsTransitioning => _isTransitioning;
        
        // Expose the current ItemsControl for external access (zoom, pan, etc.)
        public ItemsControl? CurrentItemsControl => _currentItemsControl;
        
        // Expose the transforms for zoom and pan functionality
        public ScaleTransform? CurrentScaleTransform
        {
            get
            {
                if (_currentItemsControl?.RenderTransform is TransformGroup group && group.Children.Count > 0)
                    return group.Children[0] as ScaleTransform;
                return null;
            }
        }
        
        public TranslateTransform? CurrentTranslateTransform
        {
            get
            {
                if (_currentItemsControl?.RenderTransform is TransformGroup group && group.Children.Count > 1)
                    return group.Children[1] as TranslateTransform;
                return null;
            }
        }
    }
}
