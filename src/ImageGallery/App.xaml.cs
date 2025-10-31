using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using ImageGallery.Services;
using Serilog;
using ImageGallery.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageGallery;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Check if appsettings.json exists
        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        var hasAppSettings = File.Exists(appSettingsPath);
        
        IConfiguration? configuration = null;
        var loggingEnabled = true;
        var verboseLogging = false;
        
        if (hasAppSettings)
        {
            // Build configuration from appsettings.json
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Get logging settings from configuration
            loggingEnabled = configuration.GetValue<bool>("AppSettings:Logging:Enabled", true);
            verboseLogging = configuration.GetValue<bool>("AppSettings:Logging:VerboseLogging", false);
        }

        // Create DebugLogger instance (will be shared)
        var debugLogger = new DebugLogger();

        // Configure Serilog with DebugLogger sink
        if (loggingEnabled && hasAppSettings && configuration != null)
        {
            // Use configuration from appsettings.json + add DebugLogger sink
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.DebugLogger(debugLogger);
            
            // Set minimum level based on verbose logging setting
            if (verboseLogging)
            {
                loggerConfig.MinimumLevel.Verbose();
            }
            
            Log.Logger = loggerConfig.CreateLogger();
            Log.Information(Strings.SLog_ApplicationStarting);
        }
        else if (loggingEnabled && !hasAppSettings)
        {
            // No appsettings.json - log to DebugLogger only
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.DebugLogger(debugLogger)
                .CreateLogger();
            Log.Information(Strings.SLog_ApplicationStarting);
        }
        else
        {
            // Silent logger - no output
            Log.Logger = new LoggerConfiguration()
                .CreateLogger();
        }

        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services, debugLogger);
        ServiceProvider = services.BuildServiceProvider();

        // Log unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            Log.Fatal(exception, Strings.SLog_UnhandledException);
            
            // Show error dialog
            ShowFatalErrorDialog(exception, "Unhandled Application Exception");
            
            Log.CloseAndFlush();
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            Log.Fatal(args.Exception, Strings.SLog_UnhandledDispatcherException);
            
            // Show error dialog
            ShowFatalErrorDialog(args.Exception, "Unhandled UI Exception");
            
            args.Handled = true; // Prevent crash, allow graceful shutdown
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Log.Fatal(args.Exception, "Unhandled Task Exception");
            
            // Show error dialog
            ShowFatalErrorDialog(args.Exception, "Unhandled Background Task Exception");
            
            args.SetObserved(); // Prevent crash
        };

        // Parse command-line arguments
        var cliArgs = ParseCommandLineArguments(e.Args);
        
        // Create and show main window with CLI arguments from DI
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.SetCommandLineArguments(cliArgs);
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services, DebugLogger debugLogger)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Add the shared DebugLogger instance
        services.AddSingleton(debugLogger);

        // Add services
        services.AddSingleton<ImageCache>(sp => 
        {
            var logger = sp.GetRequiredService<ILogger<ImageCache>>();
            return new ImageCache(logger, cacheSize: 64, preloadAhead: 16, keepBehind: 8);
        });
        services.AddSingleton<ImageManager>();
        services.AddSingleton<ZoomController>();
        services.AddSingleton<MosaicManager>();
        services.AddSingleton<SlideshowController>();
        services.AddSingleton<PauseController>();
        services.AddSingleton<IndicatorManager>();
        services.AddSingleton<KeyboardCommandService>();
        services.AddSingleton<WindowStateService>();
        services.AddSingleton<ImageLoaderService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<DisplayService>();
        
        // Add MainWindow as transient (created per request)
        services.AddTransient<MainWindow>();
    }

    private CommandLineArguments ParseCommandLineArguments(string[] args)
    {
        var cliArgs = new CommandLineArguments();
        var hasPatternOrMosaic = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            
            // Check if this is a flag
            if (arg.StartsWith("-"))
            {
                // Get the next value if it exists
                var value = (i + 1 < args.Length && !args[i + 1].StartsWith("-")) ? args[i + 1] : null;
                
                switch (arg.ToLower())
                {
                    case "-h":
                    case "--help":
                        ShowHelpAndExit();
                        break;
                        
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
                        if (value != null && int.TryParse(value, out var panes) && panes > 0)
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

    private void ShowHelpAndExit()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Strings.CLI_Help_Usage);
        sb.AppendLine();
        sb.AppendLine(Strings.CLI_Help_Options);
        sb.AppendLine(Strings.CLI_Help_Dir);
        sb.AppendLine(Strings.CLI_Help_Pattern);
        sb.AppendLine(Strings.CLI_Help_Mosaic);
        sb.AppendLine(Strings.CLI_Help_Fullscreen);
        sb.AppendLine(Strings.CLI_Help_Help);
        sb.AppendLine();
        sb.AppendLine(Strings.CLI_Help_Examples);
        sb.AppendLine(Strings.CLI_Help_Example1);
        sb.AppendLine(Strings.CLI_Help_Example2);
        sb.AppendLine();
        sb.AppendLine(Strings.CLI_Help_KeyboardShortcuts);
        sb.AppendLine($"  {Strings.Shortcuts_ArrowKeys} {Strings.Shortcuts_ArrowKeys_Desc}");
        sb.AppendLine($"  {Strings.Shortcuts_SpaceEnter} {Strings.Shortcuts_SpaceEnter_Desc}");
        sb.AppendLine($"  {Strings.Shortcuts_F} {Strings.Shortcuts_F_Desc}");
        sb.AppendLine($"  {Strings.Shortcuts_D} {Strings.Shortcuts_D_Desc}");
        sb.AppendLine($"  {Strings.Shortcuts_ShiftLess} {Strings.Shortcuts_ShiftLess_Desc}");
        sb.AppendLine($"  {Strings.Shortcuts_ShiftGreater} {Strings.Shortcuts_ShiftGreater_Desc}");
        sb.AppendLine($"  {Strings.Shortcuts_PlusMinus} {Strings.Shortcuts_PlusMinus_Desc}");
        sb.AppendLine($"  {Strings.Shortcuts_M} {Strings.Shortcuts_M_Desc}");
        sb.AppendLine($"  {Strings.Shortcuts_N} {Strings.Shortcuts_N_Desc}");
        sb.AppendLine($"  {Strings.Shortcuts_I} {Strings.Shortcuts_I_Desc}");
        sb.AppendLine($"  {Strings.Shortcuts_CtrlW} {Strings.Shortcuts_CtrlW_Desc}");
        sb.AppendLine($"  {Strings.Shortcuts_CtrlQ} {Strings.Shortcuts_CtrlQ_Desc}");
        
        MessageBox.Show(sb.ToString(), "ImageGallery Help", MessageBoxButton.OK, MessageBoxImage.Information);
        Environment.Exit(0);
    }

    private void ShowFatalErrorDialog(Exception? exception, string title)
    {
        try
        {
            var errorMessage = new System.Text.StringBuilder();
            errorMessage.AppendLine("A fatal error occurred and the application must close.");
            errorMessage.AppendLine();
            errorMessage.AppendLine($"Error: {exception?.Message ?? "Unknown error"}");
            errorMessage.AppendLine();
            errorMessage.AppendLine("Technical Details:");
            errorMessage.AppendLine(exception?.GetType().FullName ?? "Unknown");
            
            if (exception?.StackTrace != null)
            {
                errorMessage.AppendLine();
                errorMessage.AppendLine("Stack Trace:");
                // Limit stack trace to first 500 characters to avoid huge dialogs
                var stackTrace = exception.StackTrace;
                errorMessage.AppendLine(stackTrace.Length > 500 
                    ? stackTrace.Substring(0, 500) + "..." 
                    : stackTrace);
            }

            if (exception?.InnerException != null)
            {
                errorMessage.AppendLine();
                errorMessage.AppendLine($"Inner Exception: {exception.InnerException.Message}");
            }

            errorMessage.AppendLine();
            errorMessage.AppendLine("A detailed error log has been saved.");
            
            // Also write to a crash log file
            try
            {
                var crashLogPath = $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                System.IO.File.WriteAllText(crashLogPath, 
                    $"=== Fatal Error at {DateTime.Now} ===\n\n{exception}");
                errorMessage.AppendLine($"Crash log: {System.IO.Path.GetFullPath(crashLogPath)}");
            }
            catch
            {
                // Ignore crash log errors
            }

            MessageBox.Show(
                errorMessage.ToString(), 
                $"Fatal Error: {title}", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
        catch
        {
            // Last resort - show simple error
            MessageBox.Show(
                $"A fatal error occurred:\n{exception?.Message ?? "Unknown error"}", 
                "Fatal Error", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information(Strings.SLog_ApplicationExiting);
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
