# Image Gallery - Build Instructions

## Prerequisites
- .NET 8.0 SDK or later installed
- Download from: https://dotnet.microsoft.com/download

## Building the Single .EXE File

### Option 1: Using Command Line (Recommended)

Open PowerShell in the project directory and run:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

The compiled .exe will be located at:
```
bin\Release\net8.0-windows\win-x64\publish\ImageGallery.exe
```

### Option 2: Simplified Command

```powershell
dotnet publish -c Release
```

This uses the settings already configured in the .csproj file.

## Usage

1. Copy `ImageGallery.exe` to any folder
2. Place your image files (PNG, JPG, HEIC) in the same folder
3. Run `ImageGallery.exe`
4. The app will automatically load all images in random order

## Controls

- **Up Arrow / Right Arrow**: Next image
- **Down Arrow / Left Arrow**: Previous image
- **F Key**: Toggle fullscreen mode

## Supported Image Formats

- PNG (*.png)
- JPEG (*.jpg, *.jpeg)
- HEIC (*.heic, *.heif)
- WebP (*.webp)

## Notes

- The application has no UI elements - just the image display
- All images are loaded into memory at startup for fast navigation
- Images are automatically resized to fit the window without cropping or distortion
- The slideshow order is randomized each time you start the app
- HEIC support is provided through Windows Imaging Component (WIC)

## Troubleshooting

If you get an error about .NET not being installed:
- Install .NET 8.0 Runtime from https://dotnet.microsoft.com/download
- Or use the self-contained publish command above (creates a larger .exe but includes all dependencies)

If HEIC images don't load:
- Ensure you have the HEIF Image Extensions installed from Microsoft Store
- Or install: https://www.microsoft.com/store/productId/9PMMSR1CGPWG
