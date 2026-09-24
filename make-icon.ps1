Add-Type -AssemblyName System.Drawing

function New-Frame([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear([System.Drawing.Color]::Transparent)

    $bg = New-Object System.Drawing.Drawing2D.GraphicsPath
    $r = [int]($size * 0.18)
    $w = $size - 1
    $bg.AddArc(0, 0, $r * 2, $r * 2, 180, 90)
    $bg.AddArc($w - $r * 2, 0, $r * 2, $r * 2, 270, 90)
    $bg.AddArc($w - $r * 2, $w - $r * 2, $r * 2, $r * 2, 0, 90)
    $bg.AddArc(0, $w - $r * 2, $r * 2, $r * 2, 90, 90)
    $bg.CloseFigure()
    $brushBg = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 8, 10, 14))
    $g.FillPath($brushBg, $bg)

    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 188, 230, 255), ([Math]::Max(1, $size * 0.035)))
    $g.DrawPath($pen, $bg)

    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 188, 230, 255))
    $font = New-Object System.Drawing.Font('Segoe UI', ($size * 0.38), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = 'Center'
    $fmt.LineAlignment = 'Center'
    $rect = New-Object System.Drawing.RectangleF(0, ($size * 0.02), $size, $size)
    $g.DrawString('FPS', $font, $brush, $rect, $fmt)

    $penBar = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 90, 170, 255), ([Math]::Max(1, $size * 0.05)))
    $y = [int]($size * 0.78)
    $x0 = [int]($size * 0.22)
    $x1 = [int]($size * 0.78)
    $g.DrawLine($penBar, $x0, $y, $x1, $y)

    $g.Dispose()
    return $bmp
}

$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sizes = @(256, 64, 48, 32, 16)
$bitmaps = @()
foreach ($s in $sizes) { $bitmaps += (New-Frame $s) }

$icoPath = Join-Path $dir 'icon.ico'
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$sizes.Count)

$offset = 6 + 16 * $sizes.Count
$data = @()
foreach ($bmp in $bitmaps) {
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $ms.ToArray()
    $ms.Dispose()
    $data += ,$bytes

    $dim = $bmp.Width
    $bw.Write([byte]($(if ($dim -ge 256) { 0 } else { $dim })))
    $bw.Write([byte]($(if ($dim -ge 256) { 0 } else { $dim })))
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]32)
    $bw.Write([uint32]$bytes.Length)
    $bw.Write([uint32]$offset)
    $offset += $bytes.Length
}

foreach ($b in $data) { $bw.Write($b) }
$bw.Flush()
$fs.Close()

foreach ($bmp in $bitmaps) { $bmp.Dispose() }
Write-Host "Icon written: $icoPath"
