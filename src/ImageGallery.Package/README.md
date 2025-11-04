# MSIX Packaging Setup

This directory contains the MSIX packaging configuration for the Portable Image Gallery application.

## Files Structure

- `ImageGallery.Package.wapproj` - Windows Application Packaging Project
- `Package.appxmanifest` - MSIX application manifest
- `Images/` - Required visual assets for the MSIX package
- `Generate-PackageImages-Simple.ps1` - Script to generate placeholder images

## Current Status

? **Completed:**
- Created Windows Application Packaging Project (.wapproj)
- Generated Package.appxmanifest with proper identity and capabilities
- Created required visual assets (placeholder images with "IG" branding)
- Added project to main solution file
- Configured file type associations for image formats (JPG, PNG, GIF, BMP, TIFF, WEBP)

? **Known Issues:**
- MSBuild tooling for .wapproj files requires Visual Studio with Windows App SDK
- Current build fails due to missing MSBuild targets for Windows Application Packaging

## Building the MSIX Package

### Prerequisites
To build MSIX packages, you need:
1. Visual Studio 2022 with Windows App SDK workload
2. Windows 10 SDK (version 19041 or later)
3. Windows Application Packaging Project templates

### Manual Build Process
If automated building fails:
1. Open the solution in Visual Studio 2022
2. Set the ImageGallery.Package project as startup project
3. Select x64 platform configuration
4. Build the solution (Ctrl+Shift+B)
5. The MSIX package will be generated in `bin/x64/Debug/` or `bin/x64/Release/`

### Alternative Packaging Methods
If Visual Studio packaging doesn't work:
1. **MakeAppx.exe**: Use Windows SDK's MakeAppx tool directly
2. **Advanced Installer**: Third-party MSIX packaging tool
3. **MSIX Packaging Tool**: Microsoft's GUI tool for converting existing installers

## Deployment

The generated MSIX package can be:
- Sideloaded on Windows 10/11 systems
- Distributed through Microsoft Store (requires Store certification)
- Deployed via Microsoft Intune or other enterprise management tools
- Installed via PowerShell: `Add-AppxPackage -Path "package.msix"`

## Customization

### Visual Assets
Replace the generated placeholder images in `Images/` with custom designs:
- `Square44x44Logo.png` - Small tile and taskbar icon
- `Square150x150Logo.png` - Medium tile 
- `Wide310x150Logo.png` - Wide tile
- `SplashScreen.png` - Splash screen
- `StoreLogo.png` - Microsoft Store logo

### Capabilities
Edit `Package.appxmanifest` to add/remove capabilities:
- `runFullTrust` - Required for WPF applications
- `documentsLibrary` - Access to Documents folder
- `picturesLibrary` - Access to Pictures folder
- `removableStorage` - Access to USB drives and SD cards

### File Associations
The manifest currently supports these image formats:
- .jpg, .jpeg
- .png
- .gif
- .bmp
- .tiff, .tif
- .webp

Add more file types by editing the `FileTypeAssociation` section in the manifest.

## Next Steps

1. **Install Visual Studio**: Get VS 2022 with Windows App SDK workload
2. **Test Build**: Verify MSIX package builds successfully
3. **Custom Icons**: Replace placeholder images with app-specific designs
4. **Test Installation**: Sideload the MSIX package on test machines
5. **Store Preparation**: Prepare for Microsoft Store submission if desired