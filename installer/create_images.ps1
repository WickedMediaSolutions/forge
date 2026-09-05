Add-Type -AssemblyName System.Drawing

$icoPath = 'c:\Users\mcdor\MapMaker\EvenniaAtlas\EvenniaAtlas.ico'

# Create WizardImageFile (left banner, 164x314)
$icon = New-Object System.Drawing.Icon($icoPath, 256, 256)
$srcBmp = $icon.ToBitmap()
$destBmp = New-Object System.Drawing.Bitmap(164, 314)
$g = [System.Drawing.Graphics]::FromImage($destBmp)
$g.Clear([System.Drawing.Color]::FromArgb(45, 45, 48))
$iconSize = [Math]::Min(96, [Math]::Min(164, 314))
$x = (164 - $iconSize) / 2
$y = [Math]::Max(0, (314 - $iconSize) / 2 - 20)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($srcBmp, $x, $y, $iconSize, $iconSize)
$g.Dispose()
$destBmp.Save('c:\Users\mcdor\MapMaker\installer\WizardImageFile.bmp', [System.Drawing.Imaging.ImageFormat]::Bmp)
$destBmp.Dispose()
$icon.Dispose()
$srcBmp.Dispose()

Write-Host "WizardImageFile.bmp created: " (Get-Item 'c:\Users\mcdor\MapMaker\installer\WizardImageFile.bmp').Length "bytes"