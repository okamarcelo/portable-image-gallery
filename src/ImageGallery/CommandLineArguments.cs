namespace ImageGallery;

/// <summary>
/// Holds command-line arguments for the application.
/// </summary>
public class CommandLineArguments
{
    /// <summary>
    /// Root directory to search for images. If null, show the prompt screen.
    /// </summary>
    public string? RootDirectory { get; set; }

    /// <summary>
    /// Folder pattern to search for. Empty or null means all subdirectories.
    /// </summary>
    public string? FolderPattern { get; set; }

    /// <summary>
    /// Number of mosaic panes. Null means use default (1).
    /// </summary>
    public int? PaneCount { get; set; }

    /// <summary>
    /// Start in fullscreen mode. Default is false.
    /// </summary>
    public bool Fullscreen { get; set; }

    /// <summary>
    /// Returns true if CLI mode is active (root directory was specified).
    /// </summary>
    public bool IsCliMode => !string.IsNullOrWhiteSpace(RootDirectory);
}
