using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ImageGallery.Services;

/// <summary>
/// Manages debug logging and console display.
/// Single Responsibility: Handle log collection and console UI.
/// </summary>
public class DebugLogger
    {
        private readonly StringBuilder logBuilder = new StringBuilder();
        private Border? consoleContainer;
        private TextBlock? logTextBlock;

        public bool IsVisible { get; private set; } = false;

        public void Initialize(Border console, TextBlock textBlock)
        {
            consoleContainer = console;
            logTextBlock = textBlock;
        }

        public void Log(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            logBuilder.AppendLine(timestampedMessage);
            
            if (logTextBlock != null)
            {
                // Ensure we're on the UI thread
                if (logTextBlock.Dispatcher.CheckAccess())
                {
                    logTextBlock.Text = logBuilder.ToString();
                    AutoScrollToBottom();
                }
                else
                {
                    logTextBlock.Dispatcher.InvokeAsync(() =>
                    {
                        logTextBlock.Text = logBuilder.ToString();
                        AutoScrollToBottom();
                    });
                }
            }
        }

        public void Toggle()
        {
            IsVisible = !IsVisible;
            
            if (consoleContainer != null)
            {
                consoleContainer.Visibility = IsVisible ? Visibility.Visible : Visibility.Collapsed;
            }

            Log(IsVisible ? "Debug console shown" : "Debug console hidden");
        }

        private void AutoScrollToBottom()
        {
            if (consoleContainer?.Visibility == Visibility.Visible && logTextBlock != null)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(consoleContainer);
                scrollViewer?.ScrollToEnd();
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                return typedChild;

            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }
}