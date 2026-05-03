param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$directory = Split-Path -Parent $OutputPath
if (-not (Test-Path $directory)) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}

$bitmap = New-Object System.Drawing.Bitmap 64, 64
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

$circleBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(34, 38, 44))
$accentBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(42, 178, 112))
$accentPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(42, 178, 112)), 7
$slashPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(235, 73, 73)), 7
$slashPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$slashPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$accentPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$accentPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

$graphics.FillEllipse($circleBrush, 2, 2, 60, 60)

$micPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$micPath.AddArc(25, 10, 14, 14, 180, 90)
$micPath.AddArc(25, 10, 14, 14, 270, 90)
$micPath.AddArc(25, 24, 14, 14, 0, 90)
$micPath.AddArc(25, 24, 14, 14, 90, 90)
$micPath.CloseFigure()
$graphics.FillPath($accentBrush, $micPath)

$graphics.DrawArc($accentPen, 16, 27, 32, 22, 0, 180)
$graphics.DrawLine($accentPen, 32, 47, 32, 55)
$graphics.DrawLine($accentPen, 22, 55, 42, 55)
$graphics.DrawLine($slashPen, 16, 48, 49, 15)

$handle = $bitmap.GetHicon()
try {
    $icon = [System.Drawing.Icon]::FromHandle($handle)
    try {
        $stream = [System.IO.File]::Create($OutputPath)
        try {
            $icon.Save($stream)
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $icon.Dispose()
    }
}
finally {
    Add-Type -Namespace Native -Name User32 -MemberDefinition '[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool DestroyIcon(System.IntPtr hIcon);' -ErrorAction SilentlyContinue
    [Native.User32]::DestroyIcon($handle) | Out-Null
    $graphics.Dispose()
    $bitmap.Dispose()
    $circleBrush.Dispose()
    $accentBrush.Dispose()
    $accentPen.Dispose()
    $slashPen.Dispose()
    $micPath.Dispose()
}
