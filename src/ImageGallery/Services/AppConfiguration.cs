using System;
using System.Configuration;
using System.IO;

namespace ImageGallery.Services;

/// <summary>
/// Manages application configuration from app.config file.
/// Single Responsibility: Handle configuration file reading.
/// </summary>
public static class AppConfiguration
{
    private static bool? _fileLoggingEnabled;

    /// <summary>
    /// Check if file logging is enabled in app.config.
    /// Returns false if app.config doesn't exist or setting is not found.
    /// </summary>
    public static bool IsFileLoggingEnabled
    {
        get
        {
            if (_fileLoggingEnabled.HasValue)
                return _fileLoggingEnabled.Value;

            try
            {
                // Check if app.config exists
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageGallery.dll.config");
                if (!File.Exists(configPath))
                {
                    _fileLoggingEnabled = false;
                    return false;
                }

                // Read setting from app.config
                var value = ConfigurationManager.AppSettings["EnableFileLogging"];
                _fileLoggingEnabled = bool.TryParse(value, out var enabled) && enabled;
                return _fileLoggingEnabled.Value;
            }
            catch
            {
                _fileLoggingEnabled = false;
                return false;
            }
        }
    }

    public static string LogFilePath => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "logs",
        "imagegallery-.log"
    );
}
