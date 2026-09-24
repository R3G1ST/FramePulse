Add-Type -AssemblyName System.Drawing

$dir = Split-Path -Parent $MyInvocation.MyCommand.Path

function New-FpLogo([int]$size, [int]$radius) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $g.Clear([System.Drawing.Color]::Transparent)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $r = $radius
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

    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(70, 255, 255, 255), [float]([Math]::Max(1, $size * 0.03)))
    $g.DrawPath($pen, $path)

    $font = New-Object System.Drawing.Font('Segoe UI', ($size * 0.36), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = 'Center'
    $fmt.LineAlignment = 'Center'
    $brushW = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $tr = New-Object System.Drawing.RectangleF(0, [float]($size * 0.01), $size, $size)
    $g.DrawString('FP', $font, $brushW, $tr, $fmt)
    $g.Dispose()
    return $bmp
}

$small = New-FpLogo 55 14
$smallPath = Join-Path $dir 'wizard-small.png'
$small.Save($smallPath, [System.Drawing.Imaging.ImageFormat]::Png)
$small.Dispose()

$w = 164
$h = 314
$banner = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($banner)
$g.SmoothingMode = 'AntiAlias'
$g.TextRenderingHint = 'AntiAliasGridFit'

$bgRect = New-Object System.Drawing.Rectangle(0, 0, $w, $h)
$bg1 = [System.Drawing.Color]::FromArgb(255, 10, 14, 22)
$bg2 = [System.Drawing.Color]::FromArgb(255, 18, 24, 40)
$bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($bgRect, $bg1, $bg2, [float]90)
$g.FillRectangle($bgBrush, $bgRect)

$glowRect = New-Object System.Drawing.Rectangle([int]($w * 0.1), [int]($h * 0.22), [int]($w * 0.8), [int]($w * 0.8))
$glow = New-Object System.Drawing.Drawing2D.LinearGradientBrush($glowRect, [System.Drawing.Color]::FromArgb(60, 86, 148, 255), [System.Drawing.Color]::FromArgb(0, 155, 110, 255), [float]45)
$g.FillEllipse($glow, $glowRect)

$logoSize = 88
$logo = New-FpLogo $logoSize 22
$lx = [int](($w - $logoSize) / 2)
$ly = [int]($h * 0.28)
$g.DrawImage($logo, $lx, $ly, $logoSize, $logoSize)
$logo.Dispose()

$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = 'Center'
$fmt.LineAlignment = 'Center'

$fontTitle = New-Object System.Drawing.Font('Segoe UI', [float]15, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Point)
$brushTitle = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$titleRect = New-Object System.Drawing.RectangleF(0, [float]($ly + $logoSize + 18), $w, 30)
$g.DrawString('FramePulse', $fontTitle, $brushTitle, $titleRect, $fmt)

$fontSub = New-Object System.Drawing.Font('Segoe UI', [float]8.5, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Point)
$brushSub = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 140, 160, 190))
$subRect = New-Object System.Drawing.RectangleF(10, [float]($ly + $logoSize + 48), ($w - 20), 40)
$g.DrawString("FPS / Frame Time`nCPU / GPU Overlay", $fontSub, $brushSub, $subRect, $fmt)

$fontFoot = New-Object System.Drawing.Font('Segoe UI', [float]7.5, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Point)
$brushFoot = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 90, 100, 120))
$footRect = New-Object System.Drawing.RectangleF(0, [float]($h - 36), $w, 20)
$g.DrawString('v1.0.0 · R3G1S', $fontFoot, $brushFoot, $footRect, $fmt)

$g.Dispose()
$bannerPath = Join-Path $dir 'wizard-image.png'
$banner.Save($bannerPath, [System.Drawing.Imaging.ImageFormat]::Png)
$banner.Dispose()

Write-Host "Wizard images: $bannerPath, $smallPath"
