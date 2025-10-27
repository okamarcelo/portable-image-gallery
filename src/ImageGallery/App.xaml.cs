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

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information(Strings.SLog_ApplicationExiting);
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
