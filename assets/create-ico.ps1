Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$svgPath = "c:\src\github\portable-image-gallery\src\ImageGallery\Resources\Icons\app-icon.svg"
$icoPath = "c:\src\github\portable-image-gallery\src\ImageGallery\Resources\Icons\app-icon.ico"

# Read SVG content
$svgContent = Get-Content $svgPath -Raw

# Create temporary PNG files for different sizes
$sizes = @(16, 32, 48, 64, 128, 256)
$pngFiles = @()

foreach ($size in $sizes) {
    $pngPath = [System.IO.Path]::GetTempFileName() + ".png"
    $pngFiles += $pngPath
    
    # Use SVG rendering via WebView2 or direct bitmap conversion
    # For now, we'll use a simpler approach with System.Drawing
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    
    # Parse and render SVG (simplified - renders background and basic shapes)
    # Since full SVG parsing is complex, we'll create the icon programmatically
    
    # Background circle
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(76, 240, 244, 255))
    $graphics.FillEllipse($brush, 0, 0, $size, $size)
    
    # Scale factor
    $scale = $size / 256.0
    
    # Draw Picture Frame (simplified)
    $frameBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 65, 105, 225))
    $frameX = [int](25 * $scale)
    $frameY = [int](70 * $scale)
    $frameW = [int](54 * $scale)
    $frameH = [int](45 * $scale)
    $graphics.FillRectangle($frameBrush, $frameX, $frameY, $frameW, $frameH)
    
    # Draw Pig (center) - simplified
    $pigBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 182, 193))
    $pigX = [int]((128 - 46) * $scale)
    $pigY = [int]((128 - 43) * $scale)
    $pigW = [int](92 * $scale)
    $pigH = [int](86 * $scale)
    $graphics.FillEllipse($pigBrush, $pigX, $pigY, $pigW, $pigH)
    
    # Draw eyes
    $eyeBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 46, 75, 143))
    $leftEyeX = [int]((128 - 15) * $scale)
    $leftEyeY = [int]((128 - 8) * $scale)
    $eyeSize = [int](13 * $scale)
    $graphics.FillEllipse($eyeBrush, $leftEyeX, $leftEyeY, $eyeSize, $eyeSize)
    $rightEyeX = [int]((128 + 15 - 6) * $scale)
    $graphics.FillEllipse($eyeBrush, $rightEyeX, $leftEyeY, $eyeSize, $eyeSize)
    
    # Draw snout
    $snoutBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 209, 220))
    $snoutX = [int]((128 - 21) * $scale)
    $snoutY = [int]((128 + 8 - 8) * $scale)
    $snoutW = [int](42 * $scale)
    $snoutH = [int](32 * $scale)
    $graphics.FillEllipse($snoutBrush, $snoutX, $snoutY, $snoutW, $snoutH)
    
    # Draw Briefcase (simplified)
    $briefcaseBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 65, 105, 225))
    $briefcaseX = [int](145 * $scale)
    $briefcaseY = [int](155 * $scale)
    $briefcaseW = [int](70 * $scale)
    $briefcaseH = [int](50 * $scale)
    $graphics.FillRectangle($briefcaseBrush, $briefcaseX, $briefcaseY, $briefcaseW, $briefcaseH)
    
    $graphics.Dispose()
    
    # Save as PNG
    $bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

# Create ICO file from PNGs
$iconStream = [System.IO.File]::Create($icoPath)
$iconWriter = New-Object System.IO.BinaryWriter($iconStream)

# ICO Header
$iconWriter.Write([UInt16]0)  # Reserved
$iconWriter.Write([UInt16]1)  # Type (1 = ICO)
$iconWriter.Write([UInt16]$pngFiles.Count)  # Number of images

$dataOffset = 6 + ($pngFiles.Count * 16)

foreach ($pngPath in $pngFiles) {
    $pngData = [System.IO.File]::ReadAllBytes($pngPath)
    $pngImage = [System.Drawing.Image]::FromFile($pngPath)
    
    # Icon Directory Entry
    # For 256x256, use 0 (special case in ICO format)
    $w = if ($pngImage.Width -eq 256) { 0 } else { $pngImage.Width }
    $h = if ($pngImage.Height -eq 256) { 0 } else { $pngImage.Height }
    $iconWriter.Write([byte]$w)    # Width
    $iconWriter.Write([byte]$h)   # Height
    $iconWriter.Write([byte]0)                  # Color palette
    $iconWriter.Write([byte]0)                  # Reserved
    $iconWriter.Write([UInt16]1)                # Color planes
    $iconWriter.Write([UInt16]32)               # Bits per pixel
    $iconWriter.Write([UInt32]$pngData.Length)  # Size of image data
    $iconWriter.Write([UInt32]$dataOffset)      # Offset to image data
    
    $dataOffset += $pngData.Length
    $pngImage.Dispose()
}

# Write PNG data
foreach ($pngPath in $pngFiles) {
    $pngData = [System.IO.File]::ReadAllBytes($pngPath)
    $iconWriter.Write($pngData)
    Remove-Item $pngPath -Force
}

$iconWriter.Close()
$iconStream.Close()

Write-Host "ICO file created successfully at: $icoPath"
