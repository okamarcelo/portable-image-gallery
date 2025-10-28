using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ImageGallery.Resources;

namespace ImageGallery.Services;

/// <summary>
/// Manages debug logging and console display.
/// Single Responsibility: Handle log collection and console UI.
/// </summary>
public class DebugLogger : IDisposable
{
    private readonly StringBuilder _logBuilder = new StringBuilder();
    private Border? _consoleContainer;
    private TextBox? _logTextBox;
    private const int MaxLogLength = 100000; // Limit log to 100KB to prevent crashes

    public bool IsVisible { get; private set; } = false;

    public DebugLogger()
    {
        // No file logging - Serilog will handle that
    }

    public void Initialize(Border console, TextBox textBox)
    {
        _consoleContainer = console;
        _logTextBox = textBox;
    }

    public void Log(string message)
    {
        try
        {
            var timestampedMessage = string.Format(Strings.Log_Timestamp, DateTime.Now, message);
            
            // Add to in-memory log with size limit
            _logBuilder.AppendLine(timestampedMessage);
            
            // Trim log if too large (keep last 50KB)
            if (_logBuilder.Length > MaxLogLength)
            {
                var text = _logBuilder.ToString();
                var keepFrom = text.Length - (MaxLogLength / 2);
                _logBuilder.Clear();
                _logBuilder.AppendLine("... [earlier logs truncated] ...");
                _logBuilder.Append(text.Substring(keepFrom));
            }
            
            // Update UI
            if (_logTextBox != null)
            {
                // Ensure we're on the UI thread
                if (_logTextBox.Dispatcher.CheckAccess())
                {
                    UpdateLogTextBox();
                }
                else
                {
                    _logTextBox.Dispatcher.InvokeAsync(() => UpdateLogTextBox());
                }
            }
        }
        catch (Exception ex)
        {
            // Last resort - write error to file
            try
            {
                File.WriteAllText($"logger_error_{DateTime.Now:yyyyMMdd_HHmmss}.txt", 
                    $"Error in DebugLogger.Log: {ex}\nOriginal message: {message}");
            }
            catch
            {
                // Give up silently
            }
        }
    }

    private void UpdateLogTextBox()
    {
        try
        {
            if (_logTextBox != null)
            {
                _logTextBox.Text = _logBuilder.ToString();
                AutoScrollToBottom();
            }
        }
        catch
        {
            // Ignore UI update errors
        }
    }

    public void Toggle()
    {
        IsVisible = !IsVisible;
        
        if (_consoleContainer != null)
        {
            _consoleContainer.Visibility = IsVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        Log(IsVisible ? Strings.Status_DebugConsoleShown : Strings.Status_DebugConsoleHidden);
    }

    private void AutoScrollToBottom()
    {
        if (_consoleContainer?.Visibility == Visibility.Visible && _logTextBox != null)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(_consoleContainer);
            scrollViewer?.ScrollToEnd();
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
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

    public void Dispose()
    {
        // No resources to dispose now that file logging is handled by Serilog
    }
}
