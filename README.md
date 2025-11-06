# Portable Image Gallery

<p align="center">
  <img src="assets/pig-readme-banner.svg" alt="Portable Image Gallery - PIG" width="800"/>
</p>

A lightweight, portable WPF image viewer with slideshow capabilities, mosaic view, and internationalization support.

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- ??? **Multiple Image Formats**: PNG, JPG, JPEG, HEIC, HEIF, WebP
- ?? **Slideshow Mode**: Automatic image rotation with adjustable speed
- ?? **Mosaic View**: Display 1, 2, 4, 9, or 16 images simultaneously
- ?? **Lazy Loading**: Efficient memory management with sliding window cache
- ?? **Internationalization**: Multi-language support (English, Portuguese)
- ??? **Fullscreen Support**: Immersive viewing experience
- ? **Portable**: Single executable file, no installation required
- ?? **CLI Support**: Command-line interface for automation
- ?? **Zoom Functionality**: Zoom in/out on images
- ?? **Debug Console**: Built-in logging for troubleshooting

## Quick Start

### Download

Download the latest release from the [Releases](https://github.com/okamarcelo/portable-image-gallery/releases) page.

### Run

1. Extract `ImageGallery.exe` to any folder
2. Place your images in the same folder or subdirectories
3. Double-click `ImageGallery.exe`
4. Select the folder pattern or load all images

## Keyboard Shortcuts

### Navigation
- **Arrow Keys (?/?)**: Next image(s)
- **Arrow Keys (?/?)**: Previous image(s)
- **Home**: Go to first image
- **End**: Go to last image

### View Controls
- **F**: Toggle fullscreen mode
- **M**: Increase mosaic panes (1?2?4?9?16)
- **N**: Decrease mosaic panes
- **+**: Zoom in
- **-**: Zoom out
- **0**: Reset zoom to 100%

### Slideshow Controls
- **Space** / **Enter**: Pause/Resume slideshow
- **Shift + >**: Increase slideshow speed
- **Shift + <**: Decrease slideshow speed

### Application Controls
- **I**: Import/select different directory
- **S**: Shuffle images
- **D**: Toggle debug console
- **Ctrl+W**: Close application
- **Ctrl+Q**: Close and delete all loaded images
- **Escape**: Exit fullscreen

## Command Line Usage

```bash
ImageGallery.exe [options]
```

### Options

| Option | Description | Example |
|--------|-------------|---------|
| `-d, --dir <path>` | Root directory to search | `-d "C:\Photos"` |
| `-p, --pattern <name>` | Folder name pattern to search | `-p "vacation"` |
| `-m, --mosaic <count>` | Number of mosaic panes (1, 2, 4, 9, 16) | `-m 4` |
| `-f, --fullscreen` | Start in fullscreen mode | `-f` |

### Examples

```bash
# Load all images from a directory
ImageGallery.exe -d "C:\Photos"

# Search for specific folder pattern with 4-pane mosaic
ImageGallery.exe -d "D:\Pictures" -p "images" -m 4

# Start in fullscreen with 9-pane mosaic
ImageGallery.exe -d "E:\Vacation" -m 9 -f

# Use current directory with pattern search
cd C:\Photos
ImageGallery.exe -p "gallery" -m 2
```

## Building from Source

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later

### Build Commands

**Standard build:**
```powershell
cd src/ImageGallery
dotnet build -c Release
```

**Publish as single executable:**
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The compiled executable will be in:
```
src/ImageGallery/bin/Release/net8.0-windows/win-x64/publish/ImageGallery.exe
```

### Run Tests

```powershell
cd src/ImageGallery.Tests
dotnet test
```

## Architecture

### Lazy Loading System

The application uses a sophisticated lazy loading system with a sliding window cache:

- **Cache Size**: 64 images (configurable)
- **Preload Ahead**: 16 images
- **Keep Behind**: 8 images
- **Thread-Safe**: Uses `SemaphoreSlim` for concurrent access
- **Auto-Eviction**: Removes images outside the viewing window

### Logging System

Built on Serilog with Microsoft's `ILogger<T>` interface:

- **Structured Logging**: All logs use named properties
- **Multiple Sinks**: File + Debug Console UI
- **Dependency Injection**: Services receive logger instances
- **Configuration**: Via `appsettings.json` (optional)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Verbose",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/imagegallery-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ]
  }
}
```

### Services Architecture

The application follows a service-oriented architecture with dependency injection:

- **ImageManager**: Handles image loading and file operations
- **ImageCache**: Manages lazy loading with sliding window
- **ZoomController**: Controls zoom levels
- **MosaicManager**: Manages mosaic pane layout
- **SlideshowController**: Controls automatic slideshow
- **PauseController**: Manages pause state
- **DebugLogger**: Provides UI console for debugging
- **IndicatorManager**: Manages UI indicators

## Internationalization (i18n)

The application supports multiple languages through .NET resource files.

### Current Languages

- ???? English (default)
- ???? Portuguese (Brazil)

### Adding a New Language

1. Copy `src/ImageGallery/Resources/Strings.resx` to `Strings.[culture].resx`
   - Example: `Strings.es.resx` for Spanish
   - Example: `Strings.fr.resx` for French

2. Save with UTF-8 encoding with BOM

3. Translate all `<value>` elements while preserving format placeholders

4. Build and test:
```powershell
dotnet build
dotnet run
```

For detailed instructions, see [I18N_GUIDE.md](src/I18N_GUIDE.md).

## Supported Image Formats

| Format | Extensions | Notes |
|--------|------------|-------|
| PNG | .png | Full support |
| JPEG | .jpg, .jpeg | Full support |
| HEIC/HEIF | .heic, .heif | Requires [HEIF Image Extensions](https://www.microsoft.com/store/productId/9PMMSR1CGPWG) |
| WebP | .webp | Full support |

## Configuration

### appsettings.json (Optional)

The application works without configuration, but you can customize logging:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/imagegallery-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  },
  "AppSettings": {
    "Logging": {
      "Enabled": true,
      "VerboseLogging": false
    }
  }
}
```

Place `appsettings.json` next to `ImageGallery.exe` for file logging. Without it, logs only appear in the debug console.

## Project Structure

```
portable-image-gallery/
??? .github/                    # GitHub Actions workflows
?   ??? workflows/
?   ?   ??? build.yml          # Build and test workflow
?   ?   ??? release.yml        # Release automation
?   ??? BRANCH_PROTECTION_CHECKLIST.md
?   ??? GITHUB_SETUP.md
?   ??? SEMANTIC_VERSIONING.md
??? src/
?   ??? ImageGallery/          # Main application
?   ?   ??? App.xaml           # Application definition
?   ?   ??? MainWindow.xaml    # Main window
?   ?   ??? Resources/         # Localization resources
?   ?   ?   ??? Strings.resx
?   ?   ?   ??? Strings.pt-BR.resx
?   ?   ??? Services/          # Business logic services
?   ?       ??? ImageCache.cs
?   ?       ??? ImageManager.cs
?   ?       ??? DebugLogger.cs
?   ?       ??? DebugLoggerSink.cs
?   ?       ??? ...
?   ??? ImageGallery.Tests/   # Unit tests
?   ?   ??? Services/
?   ?       ??? ImageCacheTests.cs
?   ??? BUILD_INSTRUCTIONS.md
?   ??? CLI_USAGE.md
?   ??? I18N_GUIDE.md
?   ??? I18N_IMPLEMENTATION.md
??? README.md                  # This file
```

## CI/CD

The project uses GitHub Actions for automated build, test, and release:

- **Build and Test**: Runs on every push and PR
- **Release**: Creates releases with semantic versioning
- **PR Automation**: Automatically creates PRs for feature branches

See [.github/workflows/](. github/workflows/) for workflow definitions.

## Troubleshooting

### Images Don't Load

- Ensure image files have supported extensions (.png, .jpg, .jpeg, .heic, .heif, .webp)
- Check folder permissions
- For HEIC: Install [HEIF Image Extensions](https://www.microsoft.com/store/productId/9PMMSR1CGPWG)

### Performance Issues

- Enable lazy loading (default for 32+ images)
- Reduce mosaic pane count
- Check debug console (press `D`) for errors

### Application Crashes

- Press `D` to open debug console before crash
- Check log files in `logs/` directory (if `appsettings.json` is present)
- Check crash dumps in application directory (`crash_*.log`)

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests: `dotnet test`
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Built with [.NET 8.0](https://dotnet.microsoft.com/)
- Logging powered by [Serilog](https://serilog.net/)
- Uses [Microsoft.Extensions.Logging](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging)

## Support

For issues, questions, or feature requests, please use the [GitHub Issues](https://github.com/okamarcelo/portable-image-gallery/issues) page.
