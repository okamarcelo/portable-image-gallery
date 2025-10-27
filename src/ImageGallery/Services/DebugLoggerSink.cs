using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace ImageGallery.Services;

/// <summary>
/// Custom Serilog sink that writes to the DebugLogger UI console.
/// </summary>
public class DebugLoggerSink : ILogEventSink
{
    private readonly DebugLogger _debugLogger;
    private readonly ITextFormatter _formatter;

    public DebugLoggerSink(DebugLogger debugLogger, string outputTemplate = "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    {
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
        _formatter = new MessageTemplateTextFormatter(outputTemplate);
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

        using var writer = new System.IO.StringWriter();
        _formatter.Format(logEvent, writer);
        var message = writer.ToString().TrimEnd('\r', '\n');
        
        _debugLogger.Log(message);
    }
}

/// <summary>
/// Extension methods for configuring the DebugLogger sink.
/// </summary>
public static class DebugLoggerSinkExtensions
{
    public static LoggerConfiguration DebugLogger(
        this LoggerSinkConfiguration sinkConfiguration,
        DebugLogger debugLogger,
        string outputTemplate = "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    {
        if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
        if (debugLogger == null) throw new ArgumentNullException(nameof(debugLogger));

        return sinkConfiguration.Sink(new DebugLoggerSink(debugLogger, outputTemplate));
    }
}
