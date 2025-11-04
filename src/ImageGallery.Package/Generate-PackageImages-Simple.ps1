# Generate MSIX Package Images - Simple Version
# This script creates placeholder images for MSIX packaging when the source icon is problematic

param(
    [string]$OutputDir = "Images"
)

Write-Host "Generating MSIX Package Images (Placeholder Version)..." -ForegroundColor Green
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

try {
    # Load System.Drawing assembly
    Add-Type -AssemblyName System.Drawing

    foreach ($spec in $ImageSpecs) {
        $outputPath = Join-Path $OutputDir $spec.Name
        Write-Host "Generating $($spec.Name) ($($spec.Width)x$($spec.Height)) - $($spec.Description)" -ForegroundColor Cyan
        
        # Create new bitmap with target size
        $bitmap = New-Object System.Drawing.Bitmap($spec.Width, $spec.Height)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        
        # Set high quality rendering
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        
        # Create a gradient background
        $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            [System.Drawing.Rectangle]::new(0, 0, $spec.Width, $spec.Height),
            [System.Drawing.Color]::FromArgb(100, 149, 237),  # Cornflower Blue
            [System.Drawing.Color]::FromArgb(65, 105, 225),   # Royal Blue
            45  # 45-degree angle
        )
        
        $graphics.FillRectangle($brush, 0, 0, $spec.Width, $spec.Height)
        
        # Add text overlay
        $font = New-Object System.Drawing.Font("Segoe UI", [Math]::Max(8, $spec.Height / 10), [System.Drawing.FontStyle]::Bold)
        $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
        $text = "IG"  # Image Gallery initials
        $textSize = $graphics.MeasureString($text, $font)
        $x = ($spec.Width - $textSize.Width) / 2
        $y = ($spec.Height - $textSize.Height) / 2
        
        $graphics.DrawString($text, $font, $textBrush, $x, $y)
        
        # Save as PNG
        $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        
        # Cleanup
        $graphics.Dispose()
        $bitmap.Dispose()
        $brush.Dispose()
        $font.Dispose()
        $textBrush.Dispose()
        
        Write-Host "  ? Created: $outputPath" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "All MSIX package images generated successfully!" -ForegroundColor Green
    Write-Host "Images created in: $OutputDir" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Note: These are placeholder images with 'IG' text." -ForegroundColor Yellow
    Write-Host "Consider replacing them with custom designs for production use." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Review the generated images" -ForegroundColor White
    Write-Host "2. Replace with custom designs if needed" -ForegroundColor White
    Write-Host "3. Test the MSIX package build" -ForegroundColor White

} catch {
    Write-Error "Failed to generate images: $($_.Exception.Message)"
    Write-Host "Error details: $($_.Exception)" -ForegroundColor Red
    exit 1
}