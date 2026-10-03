Add-Type -AssemblyName System.Drawing
$assetDir = Join-Path $PSScriptRoot 'StaticAnchorOverlay/Assets'
[void][System.IO.Directory]::CreateDirectory($assetDir)
$frames = @()
foreach ($size in @(16,24,32,48,64,128,256)) {
 $bmp = [System.Drawing.Bitmap]::new($size,$size)
 $g = [System.Drawing.Graphics]::FromImage($bmp)
 $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
 $g.Clear([System.Drawing.Color]::Transparent)
 $scale = $size / 64.0
 $g.ScaleTransform($scale,$scale)
 $bg = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#14243B'))
 $accent = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#51DAD2'),4)
 $white = [System.Drawing.Pen]::new([System.Drawing.Color]::White,4)
 $g.FillEllipse($bg,2,2,60,60)
 $g.DrawArc($accent,13,13,38,38,15,60); $g.DrawArc($accent,13,13,38,38,105,60); $g.DrawArc($accent,13,13,38,38,195,60); $g.DrawArc($accent,13,13,38,38,285,60)
 $g.DrawLine($white,32,19,32,26); $g.DrawLine($white,32,38,32,45); $g.DrawLine($white,19,32,26,32); $g.DrawLine($white,38,32,45,32)
 $g.FillEllipse([System.Drawing.Brushes]::White,29,29,6,6)
 $stream = [System.IO.MemoryStream]::new(); $bmp.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
 $frames += ,@{Size=$size;Data=$stream.ToArray()}
 $stream.Dispose(); $white.Dispose(); $accent.Dispose(); $bg.Dispose(); $g.Dispose(); $bmp.Dispose()
}
$out = [System.IO.File]::Create((Join-Path $assetDir 'App.ico')); $writer = [System.IO.BinaryWriter]::new($out)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($frame in $frames) {
 $dimension = if ($frame.Size -eq 256) {0} else {$frame.Size}
 $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
 $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$frame.Data.Length); $writer.Write([uint32]$offset)
 $offset += $frame.Data.Length
}
foreach ($frame in $frames) {$writer.Write([byte[]]$frame.Data)}
$writer.Dispose()
