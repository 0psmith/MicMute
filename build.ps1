param(
    [switch]$Run
)

$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$SourceDir = Join-Path $Root 'src'
$OutputDir = Join-Path $Root 'bin'
$AssetsDir = Join-Path $Root 'assets'
$FrameworkDir = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$Compiler = Join-Path $FrameworkDir 'csc.exe'

if (-not (Test-Path $Compiler)) {
    throw "C# compiler was not found at $Compiler"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$IconPath = Join-Path $AssetsDir 'MicMute.ico'
$IconGenerator = Join-Path $Root 'tools\GenerateMicMuteIcon.ps1'
if ((-not (Test-Path $IconPath)) -and (Test-Path $IconGenerator)) {
    & $IconGenerator -OutputPath $IconPath
}

$Sources = Get-ChildItem -Path $SourceDir -Filter '*.cs' | Sort-Object Name | ForEach-Object { $_.FullName }
if ($Sources.Count -eq 0) {
    throw "No source files found in $SourceDir"
}

$References = @(
    'System.dll',
    'System.Core.dll',
    'System.Drawing.dll',
    'System.Windows.Forms.dll',
    'System.Xml.dll'
) | ForEach-Object { '/reference:' + (Join-Path $FrameworkDir $_) }

$OutputExe = Join-Path $OutputDir 'MicMute.exe'
$Arguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:x64',
    '/optimize+',
    '/codepage:65001',
    '/warn:4',
    ('/out:' + $OutputExe)
)

$Manifest = Join-Path $Root 'app.manifest'
if (Test-Path $Manifest) {
    $Arguments += ('/win32manifest:' + $Manifest)
}

if (Test-Path $IconPath) {
    $Arguments += ('/win32icon:' + $IconPath)
}

$Arguments += $References
$Arguments += $Sources

& $Compiler @Arguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Built $OutputExe"

if ($Run) {
    Start-Process -FilePath $OutputExe
}
