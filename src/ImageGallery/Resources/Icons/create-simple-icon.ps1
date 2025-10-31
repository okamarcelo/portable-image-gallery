# Simple icon creation script
Add-Type -AssemblyName System.Drawing

try {
    $bitmap = New-Object System.Drawing.Bitmap(32, 32)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    
    # Create a simple but recognizable icon
    # Blue circular background
    $blueBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(74, 144, 226))
    $graphics.FillEllipse($blueBrush, 0, 0, 32, 32)
    
    # White photo frame
    $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $graphics.FillRectangle($whiteBrush, 6, 8, 20, 15)
    
    # Gray photo content
    $grayBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Gray)
    $graphics.FillRectangle($grayBrush, 7, 9, 18, 13)
    
    # Simple mountain scene
    $darkBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::DarkSlateGray)
    $points = @(
        [System.Drawing.Point]::new(7, 22),
        [System.Drawing.Point]::new(14, 14),
        [System.Drawing.Point]::new(18, 17),
        [System.Drawing.Point]::new(25, 22)
    )
    $graphics.FillPolygon($darkBrush, $points)
    
    # Sun
    $yellowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Gold)
    $graphics.FillEllipse($yellowBrush, 19, 10, 5, 5)
    
    $graphics.Dispose()
    
    # Save as PNG first, then we'll rename to ICO
    $pngPath = "c:\src\github\portable-image-gallery\src\ImageGallery\Resources\Icons\app-icon-temp.png"
    $bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    
    # Now create ICO using built-in .NET methods
    $icoPath = "c:\src\github\portable-image-gallery\src\ImageGallery\Resources\Icons\app-icon.ico"
    
    # Simple ICO creation
    $ms = New-Object System.IO.MemoryStream
    $bitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $ms.Position = 0
    
    # Create a simple ICO file structure
    # ICO header (6 bytes)
    $icoData = [byte[]]@(0, 0, 1, 0, 1, 0)  # Reserved, Type (ICO), Count
    
    # ICO directory entry (16 bytes)
    $icoData += [byte[]]@(32, 32, 0, 0, 1, 0, 32, 0)  # Width, Height, Colors, Reserved, Planes, BitCount
    
    # Size and offset (little-endian)
    $pngBytes = $ms.ToArray()
    $size = $pngBytes.Length
    $sizeBytes = [BitConverter]::GetBytes([uint32]$size)
    $offsetBytes = [BitConverter]::GetBytes([uint32]22)  # Header(6) + DirEntry(16) = 22
    
    $icoData += $sizeBytes + $offsetBytes
    $icoData += $pngBytes
    
    [System.IO.File]::WriteAllBytes($icoPath, $icoData)
    
    # Cleanup
    $ms.Dispose()
    $bitmap.Dispose()
    $blueBrush.Dispose()
    $whiteBrush.Dispose()
    $grayBrush.Dispose()
    $darkBrush.Dispose()
    $yellowBrush.Dispose()
    
    # Remove temp PNG
    if (Test-Path $pngPath) {
        Remove-Item $pngPath
    }
    
    Write-Host "ICO file created successfully at: $icoPath"
    Write-Host "File size: $((Get-Item $icoPath).Length) bytes"
}
catch {
    Write-Error "Failed to create icon: $($_.Exception.Message)"
    Write-Error $_.Exception.StackTrace
}