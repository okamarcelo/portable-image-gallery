# PowerShell script to create a proper ICO file
Add-Type -AssemblyName System.Drawing

function Create-ProperIcoFile {
    param($icoPath)
    
    try {
        # Create a proper ICO file with multiple sizes
        $sizes = @(16, 32, 48, 64, 128, 256)
        
        # We'll create a simple but proper icon
        foreach ($size in $sizes) {
            $bitmap = New-Object System.Drawing.Bitmap($size, $size)
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            
            # Create gradient background
            $centerX = $size / 2
            $centerY = $size / 2
            $radius = ($size - 4) / 2
            
            # Background circle with gradient
            $path = New-Object System.Drawing.Drawing2D.GraphicsPath
            $path.AddEllipse(2, 2, $size - 4, $size - 4)
            
            $brush = New-Object System.Drawing.Drawing2D.PathGradientBrush($path)
            $brush.CenterColor = [System.Drawing.Color]::FromArgb(74, 144, 226)
            $brush.SurroundColors = @([System.Drawing.Color]::FromArgb(53, 122, 189))
            
            $graphics.FillPath($brush, $path)
            
            # Draw photo frames if size is adequate
            if ($size -ge 32) {
                $frameSize = [Math]::Max(8, $size / 4)
                $frameX = ($size - $frameSize) / 2
                $frameY = ($size - $frameSize) / 2
                
                # White frame
                $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
                $graphics.FillRectangle($whiteBrush, $frameX, $frameY, $frameSize, $frameSize * 0.75)
                
                # Gray content
                $grayBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::LightGray)
                $innerMargin = [Math]::Max(1, $size / 32)
                $graphics.FillRectangle($grayBrush, $frameX + $innerMargin, $frameY + $innerMargin, 
                                       $frameSize - 2 * $innerMargin, $frameSize * 0.75 - 2 * $innerMargin)
                
                # Simple scene elements if size allows
                if ($size -ge 48) {
                    # Mountain
                    $sceneBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::DarkGray)
                    $points = @(
                        [System.Drawing.Point]::new($frameX + $innerMargin, $frameY + $frameSize * 0.75 - $innerMargin),
                        [System.Drawing.Point]::new($frameX + $frameSize * 0.4, $frameY + $frameSize * 0.3),
                        [System.Drawing.Point]::new($frameX + $frameSize * 0.7, $frameY + $frameSize * 0.5),
                        [System.Drawing.Point]::new($frameX + $frameSize - $innerMargin, $frameY + $frameSize * 0.75 - $innerMargin)
                    )
                    $graphics.FillPolygon($sceneBrush, $points)
                    
                    # Sun
                    if ($size -ge 64) {
                        $sunBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Gold)
                        $sunSize = [Math]::Max(2, $size / 16)
                        $graphics.FillEllipse($sunBrush, $frameX + $frameSize - $sunSize - 2, $frameY + 2, $sunSize, $sunSize)
                        $sunBrush.Dispose()
                    }
                    
                    $sceneBrush.Dispose()
                }
                
                $whiteBrush.Dispose()
                $grayBrush.Dispose()
            }
            
            $graphics.Dispose()
            $brush.Dispose()
            $path.Dispose()
            
            # Save the largest size as ICO - this is a simpler approach
            if ($size -eq 32) {
                # Convert to Icon and save
                $iconHandle = $bitmap.GetHicon()
                $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
                
                $fileStream = [System.IO.File]::Create($icoPath)
                $icon.Save($fileStream)
                $fileStream.Close()
                
                $icon.Dispose()
                [System.Runtime.InteropServices.Marshal]::DestroyIcon($iconHandle)
            }
            
            $bitmap.Dispose()
        }
        
        Write-Host "Proper ICO file created successfully at: $icoPath"
        return $true
    }
    catch {
        Write-Error "Failed to create ICO file: $($_.Exception.Message)"
        return $false
    }
}

# Add necessary Windows API calls
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public class Win32 {
    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
"@

$icoPath = "c:\src\github\portable-image-gallery\src\ImageGallery\Resources\Icons\app-icon.ico"

# Remove old file if exists
if (Test-Path $icoPath) {
    Remove-Item $icoPath -Force
}

$result = Create-ProperIcoFile -icoPath $icoPath

if (-not $result) {
    Write-Host "Trying alternative method..."
    
    # Alternative: Use .NET built-in icon creation
    try {
        $bitmap = New-Object System.Drawing.Bitmap(32, 32)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        
        # Simple but effective design
        $blueBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(74, 144, 226))
        $graphics.FillEllipse($blueBrush, 2, 2, 28, 28)
        
        $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
        $graphics.FillRectangle($whiteBrush, 8, 10, 16, 12)
        
        $grayBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Gray)
        $graphics.FillRectangle($grayBrush, 9, 11, 14, 10)
        
        # Create mountain shape
        $darkBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::DarkGray)
        $points = @(
            [System.Drawing.Point]::new(9, 21),
            [System.Drawing.Point]::new(15, 15),
            [System.Drawing.Point]::new(19, 18),
            [System.Drawing.Point]::new(23, 21)
        )
        $graphics.FillPolygon($darkBrush, $points)
        
        # Sun
        $yellowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Gold)
        $graphics.FillEllipse($yellowBrush, 18, 12, 4, 4)
        
        $graphics.Dispose()
        
        # Convert to icon
        $iconHandle = $bitmap.GetHicon()
        $icon = [System.Drawing.Icon]::FromHandle($iconHandle)
        
        $fileStream = [System.IO.File]::Create($icoPath)
        $icon.Save($fileStream)
        $fileStream.Close()
        
        # Cleanup
        $icon.Dispose()
        [System.Runtime.InteropServices.Marshal]::DestroyIcon($iconHandle)
        $bitmap.Dispose()
        $blueBrush.Dispose()
        $whiteBrush.Dispose()
        $grayBrush.Dispose()
        $darkBrush.Dispose()
        $yellowBrush.Dispose()
        
        Write-Host "Alternative ICO file created successfully!"
    }
    catch {
        Write-Error "Alternative method also failed: $($_.Exception.Message)"
    }
}