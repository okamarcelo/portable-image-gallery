# Generate MSIX Package Images
# This script creates the required image assets for MSIX packaging from the main app icon

param(
    [string]$SourceIcon = "..\ImageGallery\Resources\Icons\app-icon.ico",
    [string]$OutputDir = "Images"
)

Write-Host "Generating MSIX Package Images..." -ForegroundColor Green
Write-Host "Source Icon: $SourceIcon" -ForegroundColor Yellow
Write-Host "Output Directory: $OutputDir" -ForegroundColor Yellow

# Ensure output directory exists
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "Created output directory: $OutputDir" -ForegroundColor Cyan
}

# Define required image sizes and names
$ImageSpecs = @(
    @{ Name = "Square44x44Logo.png"; Width = 44; Height = 44; Description = "Small tile and taskbar icon" },
    @{ Name = "Square150x150Logo.png"; Width = 150; Height = 150; Description = "Medium tile" },
    @{ Name = "Wide310x150Logo.png"; Width = 310; Height = 150; Description = "Wide tile" },
    @{ Name = "SplashScreen.png"; Width = 620; Height = 300; Description = "Splash screen" },
    @{ Name = "StoreLogo.png"; Width = 50; Height = 50; Description = "Microsoft Store logo" }
)

# Check if source icon exists
if (!(Test-Path $SourceIcon)) {
    Write-Error "Source icon not found: $SourceIcon"
    Write-Host "Please ensure the app icon exists at the specified path." -ForegroundColor Red
    exit 1
}

try {
    # Load System.Drawing assembly
    Add-Type -AssemblyName System.Drawing

    # Load the source icon
    Write-Host "Loading source icon..." -ForegroundColor Cyan
    $icon = [System.Drawing.Icon]::new($SourceIcon)
    $bitmap = $icon.ToBitmap()

    foreach ($spec in $ImageSpecs) {
        $outputPath = Join-Path $OutputDir $spec.Name
        Write-Host "Generating $($spec.Name) ($($spec.Width)x$($spec.Height)) - $($spec.Description)" -ForegroundColor Cyan
        
        # Create new bitmap with target size
        $resizedBitmap = New-Object System.Drawing.Bitmap($spec.Width, $spec.Height)
        $graphics = [System.Drawing.Graphics]::FromImage($resizedBitmap)
        
        # Set high quality rendering
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        
        # For wide tile and splash screen, center the icon
        if ($spec.Name -eq "Wide310x150Logo.png" -or $spec.Name -eq "SplashScreen.png") {
            # Calculate centered position
            $iconSize = [Math]::Min($spec.Height * 0.8, $spec.Width * 0.4)
            $x = ($spec.Width - $iconSize) / 2
            $y = ($spec.Height - $iconSize) / 2
            $graphics.DrawImage($bitmap, $x, $y, $iconSize, $iconSize)
        } else {
            # For square images, fill the entire area
            $graphics.DrawImage($bitmap, 0, 0, $spec.Width, $spec.Height)
        }
        
        # Save as PNG
        $resizedBitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        
        # Cleanup
        $graphics.Dispose()
        $resizedBitmap.Dispose()
        
        Write-Host "  ? Created: $outputPath" -ForegroundColor Green
    }

    # Cleanup
    $bitmap.Dispose()
    $icon.Dispose()

    Write-Host ""
    Write-Host "All MSIX package images generated successfully!" -ForegroundColor Green
    Write-Host "Images created in: $OutputDir" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Review the generated images and adjust if needed" -ForegroundColor White
    Write-Host "2. Consider creating custom designs for Wide310x150Logo.png and SplashScreen.png" -ForegroundColor White
    Write-Host "3. Test the MSIX package build" -ForegroundColor White

} catch {
    Write-Error "Failed to generate images: $($_.Exception.Message)"
    Write-Host "Error details: $($_.Exception)" -ForegroundColor Red
    exit 1
}