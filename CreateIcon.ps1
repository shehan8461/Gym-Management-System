# PowerShell Script to Create Gym Icon
# This creates a simple gym-themed ICO file

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

# Create a 256x256 bitmap
$size = 256
$bitmap = New-Object System.Drawing.Bitmap($size, $size)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Enable anti-aliasing for smooth drawing
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Fill background with teal gradient
$gradientBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point(0, 0)),
    (New-Object System.Drawing.Point($size, $size)),
    [System.Drawing.Color]::FromArgb(255, 0, 137, 123),  # #00897B
    [System.Drawing.Color]::FromArgb(255, 0, 191, 165)   # #00BFA5
)
$graphics.FillRectangle($gradientBrush, 0, 0, $size, $size)

# Draw dumbbell (simplified representation)
$whitePen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 12)
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

# Center bar of dumbbell
$graphics.DrawLine($whitePen, 80, 128, 176, 128)

# Left weight circles
$graphics.FillEllipse($whiteBrush, 30, 88, 60, 80)
$graphics.FillEllipse($whiteBrush, 40, 98, 40, 60)

# Right weight circles  
$graphics.FillEllipse($whiteBrush, 166, 88, 60, 80)
$graphics.FillEllipse($whiteBrush, 176, 98, 40, 60)

# Draw "GYM" text
$font = New-Object System.Drawing.Font("Arial", 48, [System.Drawing.FontStyle]::Bold)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center

$graphics.DrawString("GYM", $font, $textBrush, 128, 200, $format)

# Clean up
$graphics.Dispose()

# Save as PNG first
$pngPath = Join-Path $PSScriptRoot "Resources\gym-icon.png"
$icoPath = Join-Path $PSScriptRoot "Resources\gym-icon.ico"

# Create Resources directory if it doesn't exist
$resourcesDir = Join-Path $PSScriptRoot "Resources"
if (-not (Test-Path $resourcesDir)) {
    New-Item -ItemType Directory -Path $resourcesDir -Force | Out-Null
}

$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "PNG saved to: $pngPath" -ForegroundColor Green

# Convert PNG to ICO using .NET
# Create multiple sizes for ICO
$sizes = @(256, 128, 64, 48, 32, 16)
$stream = New-Object System.IO.MemoryStream

# ICO file header
$writer = New-Object System.IO.BinaryWriter($stream)
$writer.Write([UInt16]0)  # Reserved
$writer.Write([UInt16]1)  # Type (1 = ICO)
$writer.Write([UInt16]$sizes.Count)  # Number of images

$imageDataOffset = 6 + ($sizes.Count * 16)  # Header + directory entries
$imageDataList = @()

foreach ($iconSize in $sizes) {
    # Create resized bitmap
    $resized = New-Object System.Drawing.Bitmap($iconSize, $iconSize)
    $g = [System.Drawing.Graphics]::FromImage($resized)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($bitmap, 0, 0, $iconSize, $iconSize)
    $g.Dispose()
    
    # Save to memory stream
    $ms = New-Object System.IO.MemoryStream
    $resized.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $imageData = $ms.ToArray()
    $ms.Dispose()
    $resized.Dispose()
    
    # Write directory entry
    if ($iconSize -eq 256) {
        $writer.Write([Byte]0)  # Width (0 = 256)
        $writer.Write([Byte]0)  # Height (0 = 256)
    } else {
        $writer.Write([Byte]$iconSize)  # Width
        $writer.Write([Byte]$iconSize)  # Height
    }
    $writer.Write([Byte]0)  # Color palette
    $writer.Write([Byte]0)  # Reserved
    $writer.Write([UInt16]1)  # Color planes
    $writer.Write([UInt16]32)  # Bits per pixel
    $writer.Write([UInt32]$imageData.Length)  # Image data size
    $writer.Write([UInt32]$imageDataOffset)  # Image data offset
    
    $imageDataList += $imageData
    $imageDataOffset += $imageData.Length
}

# Write image data
foreach ($imageData in $imageDataList) {
    $writer.Write($imageData)
}

$writer.Flush()
$icoBytes = $stream.ToArray()
[System.IO.File]::WriteAllBytes($icoPath, $icoBytes)

$writer.Dispose()
$stream.Dispose()
$bitmap.Dispose()

Write-Host "ICO saved to: $icoPath" -ForegroundColor Green
Write-Host ""
Write-Host "Icon created successfully!" -ForegroundColor Cyan
Write-Host "The icon will be embedded in the next build." -ForegroundColor Yellow
