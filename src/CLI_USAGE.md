# ImageGallery - Command Line Interface (CLI) Usage

## Overview
ImageGallery can be launched from the command line with optional parameters to automatically load images without using the GUI dialogs.

## Syntax
```
ImageGallery.exe [-d|--dir <directory>] [-p|--pattern <pattern>] [-m|--mosaic <count>]
```

## Parameters

### -d, --dir <directory> (Required for CLI mode)
The root directory path to search for images.

- **If not provided**: The application starts with the regular GUI prompt screen
- **If provided**: Enables CLI mode and automatically loads images from the specified directory

**Examples:**
- `-d "C:\Photos"`
- `--dir "D:\MyPictures\Vacation2024"`
- `-d "\\NetworkShare\Images"`

### -p, --pattern <pattern> (Optional)
The folder name pattern to search for within the root directory.

- **Default (if not provided)**: Empty string - searches ALL subdirectories recursively
- **Can be used without value**: `-p` or `--pattern` without a following value searches all subdirectories
- **Examples**: `-p images`, `--pattern photos`, `-p gallery`, `-p _`

**Behavior:**
- If omitted or no value: Searches through all subdirectories for image files
- If specified: Only searches for folders with that exact name (e.g., all folders named "images")

### -m, --mosaic <count> (Optional)
The number of mosaic panes to display simultaneously.

- **Default (if not provided)**: 1 pane (single image view)
- **Valid values**: Any positive integer
- **Actual values**: Will use the closest valid mosaic size: 1, 2, 4, 9, or 16

**Examples:**
- `-m 1` - Single image
- `-m 2` or `--mosaic 2` - Two panes (side-by-side in landscape, stacked in portrait)
- `-m 4` - 2x2 grid
- `-m 9` - 3x3 grid
- `-m 16` - 4x4 grid

## Usage Examples

### Example 1: GUI Mode (No arguments)
```
ImageGallery.exe
```
Starts the application with the standard GUI prompt to select a directory.

### Example 2: Load all images from a directory
```
ImageGallery.exe -d "C:\Photos"
ImageGallery.exe --dir "C:\Photos"
```
Loads all images from all subdirectories under `C:\Photos` with 1 pane.

### Example 3: Search for specific folder pattern
```
ImageGallery.exe -d "D:\MyPictures" -p "images"
ImageGallery.exe --dir "D:\MyPictures" --pattern "images"
```
Searches for all folders named "images" under `D:\MyPictures` and loads images from them, displaying 1 pane.

### Example 4: Load with 4-pane mosaic view
```
ImageGallery.exe -d "C:\Vacation" -m 4
ImageGallery.exe --dir "C:\Vacation" --mosaic 4
```
Loads all images from all subdirectories under `C:\Vacation` in a 2x2 mosaic grid.

### Example 5: Specific pattern with 9 panes
```
ImageGallery.exe -d "E:\PhotoLibrary" -p "gallery" -m 9
ImageGallery.exe --dir "E:\PhotoLibrary" --pattern "gallery" --mosaic 9
```
Searches for folders named "gallery" under `E:\PhotoLibrary` and displays in a 3x3 mosaic grid.

### Example 6: Network path with custom pattern and 2 panes
```
ImageGallery.exe -d "\\NAS\SharedPhotos" -p "photos" -m 2
ImageGallery.exe --dir "\\NAS\SharedPhotos" --pattern "photos" --mosaic 2
```
Loads images from folders named "photos" on a network share with 2-pane view.

### Example 7: Mixed short and long options
```
ImageGallery.exe --dir "C:\Photos" -p "images" --mosaic 4
```
You can mix short and long option names as preferred.

### Example 8: Pattern flag without value (search all subdirectories)
```
ImageGallery.exe -d "C:\Photos" -p -m 9
```
Explicitly use the pattern flag without a value to search all subdirectories with 9 panes.

### Example 9: Any order of parameters
```
ImageGallery.exe -m 4 -p "photos" -d "C:\MyPictures"
ImageGallery.exe --mosaic 2 --dir "D:\Images" --pattern "gallery"
```
Parameters can be provided in any order.

## Notes

1. **Paths with spaces**: Wrap paths in quotes if they contain spaces
2. **Pattern matching**: Folder pattern is case-sensitive and must match exactly
3. **Empty pattern**: Omit `-p`/`--pattern` or use it without a value to search all subdirectories
4. **Parameter order**: Parameters can be in any order (no longer positional)
5. **Short vs long options**: Use `-d`, `-p`, `-m` for short form or `--dir`, `--pattern`, `--mosaic` for long form
6. **Case insensitive flags**: Flags are case-insensitive (`-D` and `-d` are equivalent)
7. **Error handling**: If the directory doesn't exist or contains no images, the application will show an error message
8. **Interactive mode**: Even in CLI mode, you can still use keyboard shortcuts to change settings (press `I` to select a different directory)

## Keyboard Shortcuts (Available in all modes)

- **I**: Import/select a different root directory
- **M/N**: Increase/decrease mosaic panes
- **Space/Enter**: Pause/resume slideshow
- **Arrow keys**: Navigate images manually
- **F**: Toggle fullscreen
- **+/-**: Zoom in/out (when not in mosaic mode)
- **Ctrl+W**: Close application
- **Ctrl+Q**: Close and delete all images
- **Shift+</>**: Adjust slideshow speed

## Return to GUI Mode

If you start the application with CLI parameters and want to select a different directory:
- Press **I** key to open the folder pattern and directory selection dialogs
