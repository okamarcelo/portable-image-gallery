# MSIX Package Images

This directory contains the required image assets for the MSIX package.

## Required Images:

- **Square44x44Logo.png** (44x44) - Small tile and taskbar icon
- **Square150x150Logo.png** (150x150) - Medium tile 
- **Wide310x150Logo.png** (310x150) - Wide tile
- **SplashScreen.png** (620x300) - Splash screen image
- **StoreLogo.png** (50x50) - Microsoft Store logo

## Notes:

- These images should be created from the main application icon
- Use transparent backgrounds where appropriate
- Follow Microsoft Store guidelines for image requirements
- The current placeholders should be replaced with proper branded images

## Generating Images:

You can use tools like:
- **Paint.NET** with plugins for resizing
- **GIMP** for advanced editing
- **PowerShell** scripts for batch conversion
- **Visual Studio** asset generator
- Online tools like **App Icon Generator**

The main app icon is located at: `../ImageGallery/Resources/Icons/app-icon.ico`