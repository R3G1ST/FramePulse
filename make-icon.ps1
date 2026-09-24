Add-Type -AssemblyName System.Drawing

function New-Frame([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear([System.Drawing.Color]::Transparent)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $r = [int]($size * 0.22)
    $w = $size - 1
    $path.AddArc(0, 0, $r * 2, $r * 2, 180, 90)
    $path.AddArc($w - $r * 2, 0, $r * 2, $r * 2, 270, 90)
    $path.AddArc($w - $r * 2, $w - $r * 2, $r * 2, $r * 2, 0, 90)
    $path.AddArc(0, $w - $r * 2, $r * 2, $r * 2, 90, 90)
    $path.CloseFigure()

    $c1 = [System.Drawing.Color]::FromArgb(255, 86, 148, 255)
    $c2 = [System.Drawing.Color]::FromArgb(255, 155, 110, 255)
    $rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, [float]45)
    $g.FillPath($brush, $path)

    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(70, 255, 255, 255), ([Math]::Max(1, $size * 0.025)))
    $g.DrawPath($pen, $path)

    $font = New-Object System.Drawing.Font('Segoe UI', ($size * 0.36), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = 'Center'
    $fmt.LineAlignment = 'Center'
    $brushW = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $textRect = New-Object System.Drawing.RectangleF(0, [float]($size * 0.01), $size, $size)
    $g.DrawString('FP', $font, $brushW, $textRect, $fmt)

    $g.Dispose()
    return $bmp
}

$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sizes = @(256, 64, 48, 32, 16)
$bitmaps = @()
foreach ($s in $sizes) { $bitmaps += (New-Frame $s) }

$pngPath = Join-Path $dir 'icon.png'
$bitmaps[0].Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "PNG written: $pngPath"

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
