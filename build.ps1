param(
    [string]$Version,
    [switch]$Run
)

$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$SourceDir = Join-Path $Root 'src'
$OutputDir = Join-Path $Root 'bin'
$GeneratedDir = Join-Path $Root 'obj\generated'
$AssetsDir = Join-Path $Root 'assets'
$FrameworkDir = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$Compiler = Join-Path $FrameworkDir 'csc.exe'

if (-not (Test-Path $Compiler)) {
    throw "C# compiler was not found at $Compiler"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $GeneratedDir | Out-Null

$IconPath = Join-Path $AssetsDir 'MicMute.ico'
$IconGenerator = Join-Path $Root 'tools\GenerateMicMuteIcon.ps1'
if ((-not (Test-Path $IconPath)) -and (Test-Path $IconGenerator)) {
    & $IconGenerator -OutputPath $IconPath
}

$VersionFile = Join-Path $Root 'VERSION'
if ([string]::IsNullOrWhiteSpace($Version)) {
    if (-not [string]::IsNullOrWhiteSpace($env:MICMUTE_VERSION)) {
        $Version = $env:MICMUTE_VERSION
    }
    elseif (Test-Path $VersionFile) {
        $Version = (Get-Content -Path $VersionFile -Raw).Trim()
    }
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = '0.1.0'
}

$InformationalVersion = $Version.Trim()
if ($InformationalVersion.StartsWith('v', [System.StringComparison]::OrdinalIgnoreCase)) {
    $InformationalVersion = $InformationalVersion.Substring(1)
}

$NumericVersion = ($InformationalVersion -split '[+-]', 2)[0]
if ($NumericVersion -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    throw "Version '$InformationalVersion' must look like 1.2.3 or 1.2.3.4, with optional prerelease/build metadata."
}

$VersionParts = $NumericVersion.Split('.')
$Major = [int]$VersionParts[0]
$Minor = [int]$VersionParts[1]
$Patch = [int]$VersionParts[2]
$Revision = if ($VersionParts.Count -ge 4) { [int]$VersionParts[3] } else { 0 }
$AssemblyVersion = "$Major.$Minor.$Patch.$Revision"

$AssemblyInfoPath = Join-Path $GeneratedDir 'AssemblyInfo.g.cs'
$AssemblyInfo = @"
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("MicMute")]
[assembly: AssemblyDescription("Windows microphone mute controller")]
[assembly: AssemblyCompany("0psmith")]
[assembly: AssemblyProduct("MicMute")]
[assembly: AssemblyCopyright("Copyright (c) 2026 0psmith")]
[assembly: AssemblyVersion("$AssemblyVersion")]
[assembly: AssemblyFileVersion("$AssemblyVersion")]
[assembly: AssemblyInformationalVersion("$InformationalVersion")]
[assembly: ComVisible(false)]
"@
Set-Content -Path $AssemblyInfoPath -Value $AssemblyInfo -Encoding UTF8

$Sources = @()
$Sources += Get-ChildItem -Path $SourceDir -Filter '*.cs' | Sort-Object Name | ForEach-Object { $_.FullName }
$Sources += $AssemblyInfoPath
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
Write-Host "Version $InformationalVersion"

if ($Run) {
    Start-Process -FilePath $OutputExe
}
