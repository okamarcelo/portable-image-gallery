using System;
using System.Windows;
using ImageGallery.Services;
using Serilog;

namespace ImageGallery;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure Serilog based on app.config
        if (AppConfiguration.IsFileLoggingEnabled)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(
                    AppConfiguration.LogFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Application starting with file logging enabled");
        }
        else
        {
            // Silent logger - no output
            Log.Logger = new LoggerConfiguration()
                .CreateLogger();
        }

        // Log unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception, "Unhandled exception occurred");
            Log.CloseAndFlush();
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            Log.Fatal(args.Exception, "Unhandled dispatcher exception");
            args.Handled = false; // Let it crash so user can see the error
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application exiting");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
