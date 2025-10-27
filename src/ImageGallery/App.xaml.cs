using System;
using System.Windows;
using ImageGallery.Services;
using Serilog;
using ImageGallery.Resources;

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

            Log.Information(Strings.SLog_ApplicationStarting);
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
            Log.Fatal(args.ExceptionObject as Exception, Strings.SLog_UnhandledException);
            Log.CloseAndFlush();
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            Log.Fatal(args.Exception, Strings.SLog_UnhandledDispatcherException);
            args.Handled = false; // Let it crash so user can see the error
        };

        // Parse command-line arguments
        var cliArgs = ParseCommandLineArguments(e.Args);
        
        // Create and show main window with CLI arguments
        var mainWindow = new MainWindow(cliArgs);
        mainWindow.Show();
    }

    private CommandLineArguments ParseCommandLineArguments(string[] args)
    {
        var cliArgs = new CommandLineArguments();
        bool hasPatternOrMosaic = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            
            // Check if this is a flag
            if (arg.StartsWith("-"))
            {
                // Get the next value if it exists
                string? value = (i + 1 < args.Length && !args[i + 1].StartsWith("-")) ? args[i + 1] : null;
                
                switch (arg.ToLower())
                {
                    case "-d":
                    case "--dir":
                        if (value != null)
                        {
                            cliArgs.RootDirectory = value;
                            Log.Information(Strings.SLog_RootDirectory, cliArgs.RootDirectory);
                            i++; // Skip the next argument since we consumed it
                        }
                        else
                        {
                            Log.Warning(Strings.SLog_ArgumentRequiresValue, arg);
                        }
                        break;
                        
                    case "-p":
                    case "--pattern":
                        hasPatternOrMosaic = true;
                        // Allow empty value for pattern (means all subdirectories)
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            cliArgs.FolderPattern = args[i + 1];
                            i++; // Skip the next argument
                        }
                        else
                        {
                            cliArgs.FolderPattern = ""; // Empty pattern
                        }
                        Log.Information(Strings.SLog_FolderPattern, cliArgs.FolderPattern);
                        break;
                        
                    case "-m":
                    case "--mosaic":
                        hasPatternOrMosaic = true;
                        if (value != null && int.TryParse(value, out int panes) && panes > 0)
                        {
                            cliArgs.PaneCount = panes;
                            Log.Information(Strings.SLog_PaneCount, cliArgs.PaneCount);
                            i++; // Skip the next argument since we consumed it
                        }
                        else
                        {
                            Log.Warning(Strings.SLog_ArgumentRequiresPositiveInteger, arg);
                        }
                        break;
                        
                    case "-f":
                    case "--fullscreen":
                        hasPatternOrMosaic = true;
                        cliArgs.Fullscreen = true;
                        Log.Information(Strings.SLog_FullscreenEnabled);
                        break;
                        
                    default:
                        Log.Warning(Strings.SLog_UnknownArgument, arg);
                        break;
                }
            }
        }

        // If -p or -m was specified but -d was not, use current directory
        if (string.IsNullOrWhiteSpace(cliArgs.RootDirectory) && hasPatternOrMosaic)
        {
            cliArgs.RootDirectory = Environment.CurrentDirectory;
            Log.Information(Strings.SLog_RootDirectoryNotSpecified, cliArgs.RootDirectory);
        }

        return cliArgs;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information(Strings.SLog_ApplicationExiting);
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
