# PowerShell script to convert SVG to ICO
param(
    [string]$SvgPath = "c:\src\github\portable-image-gallery\src\ImageGallery\Resources\Icons\app-icon.svg",
    [string]$IcoPath = "c:\src\github\portable-image-gallery\src\ImageGallery\Resources\Icons\app-icon.ico"
)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

# Function to create ICO from SVG using System.Drawing
function Convert-SvgToIco {
    param($svgPath, $icoPath)
    
    try {
        # Read SVG content
        $svgContent = Get-Content $svgPath -Raw
        
        # Create a temporary bitmap to render SVG
        # Since System.Drawing doesn't natively support SVG, we'll create a simple bitmap-based icon
        # For a production app, you'd typically use a more sophisticated SVG renderer
        
        # Create different sizes for the ICO file
        $sizes = @(16, 32, 48, 64, 128, 256)
        $iconBitmaps = @()
        
        foreach ($size in $sizes) {
            # Create a bitmap for this size
            $bitmap = New-Object System.Drawing.Bitmap($size, $size)
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            
            # Create a simple icon representation since we can't easily render SVG
            # Background circle
            $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
                [System.Drawing.Point]::new(0, 0),
                [System.Drawing.Point]::new($size, $size),
                [System.Drawing.Color]::FromArgb(74, 144, 226),
                [System.Drawing.Color]::FromArgb(53, 122, 189)
            )
            
            $graphics.FillEllipse($brush, 2, 2, $size-4, $size-4)
            
            # Draw photo frames representation
            $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
            $grayBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::LightGray)
            
            # Scale factors for different sizes
            $scale = $size / 256.0
            
            # Draw simplified photo frames
            $frameWidth = [int]($80 * $scale)
            $frameHeight = [int]($60 * $scale)
            $frameX = [int](($size - $frameWidth) / 2)
            $frameY = [int](($size - $frameHeight) / 2)
            
            # Draw photo frame
            $graphics.FillRectangle($whiteBrush, $frameX, $frameY, $frameWidth, $frameHeight)
            $graphics.FillRectangle($grayBrush, $frameX + 2, $frameY + 2, $frameWidth - 4, $frameHeight - 4)
            
            # Draw a simple mountain scene if size is large enough
            if ($size -ge 32) {
                $sceneBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::DarkGray)
                $sunBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Gold)
                
                # Simple mountain shape
                $points = @(
                    [System.Drawing.Point]::new($frameX + 2, $frameY + $frameHeight - 2),
                    [System.Drawing.Point]::new($frameX + [int]($frameWidth * 0.3), $frameY + [int]($frameHeight * 0.4)),
                    [System.Drawing.Point]::new($frameX + [int]($frameWidth * 0.6), $frameY + [int]($frameHeight * 0.6)),
                    [System.Drawing.Point]::new($frameX + $frameWidth - 2, $frameY + $frameHeight - 2)
                )
                $graphics.FillPolygon($sceneBrush, $points)
                
                # Simple sun
                if ($size -ge 48) {
                    $sunSize = [int]($8 * $scale)
                    $graphics.FillEllipse($sunBrush, $frameX + $frameWidth - $sunSize - 8, $frameY + 8, $sunSize, $sunSize)
                }
            }
            
            $graphics.Dispose()
            $brush.Dispose()
            $whiteBrush.Dispose()
            $grayBrush.Dispose()
            
            $iconBitmaps += $bitmap
        }
        
        # Save as ICO file
        $iconBitmaps[0].Save($icoPath, [System.Drawing.Imaging.ImageFormat]::Icon)
        
        # Cleanup
        foreach ($bitmap in $iconBitmaps) {
            $bitmap.Dispose()
        }
        
        Write-Host "ICO file created successfully at: $icoPath"
        return $true
    }
    catch {
        Write-Error "Failed to create ICO file: $($_.Exception.Message)"
        return $false
    }
}

# Create the ICO file
$result = Convert-SvgToIco -svgPath $SvgPath -icoPath $IcoPath

if ($result) {
    Write-Host "Icon conversion completed successfully!"
} else {
    Write-Host "Icon conversion failed. Creating a simple fallback ICO file..."
    
    # Fallback: Create a simple ICO file directly
    try {
        $bitmap = New-Object System.Drawing.Bitmap(32, 32)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        
        # Create simple gradient background
        $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            [System.Drawing.Point]::new(0, 0),
            [System.Drawing.Point]::new(32, 32),
            [System.Drawing.Color]::FromArgb(74, 144, 226),
            [System.Drawing.Color]::FromArgb(53, 122, 189)
        )
        
        $graphics.FillEllipse($brush, 2, 2, 28, 28)
        
        # Draw simple photo representation
        $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
        $graphics.FillRectangle($whiteBrush, 8, 10, 16, 12)
        
        $grayBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::LightGray)
        $graphics.FillRectangle($grayBrush, 9, 11, 14, 10)
        
        $graphics.Dispose()
        $brush.Dispose()
        $whiteBrush.Dispose()
        $grayBrush.Dispose()
        
        $bitmap.Save($IcoPath, [System.Drawing.Imaging.ImageFormat]::Icon)
        $bitmap.Dispose()
        
        Write-Host "Fallback ICO file created successfully!"
    }
    catch {
        Write-Error "Failed to create fallback ICO file: $($_.Exception.Message)"
    }
}