param(
    [ValidateSet('MenuLab', 'BootstrapPreview')]
    [string]$Build = 'MenuLab',
    [switch]$CheckOnly
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$executableName = if ($Build -eq 'MenuLab') { 'WaveLab.exe' } else { 'BootstrapPreview.exe' }
$executable = Join-Path $workspace "Zombie Shoot VR/Builds/Windows/$Build/$executableName"
if (!(Test-Path -LiteralPath $executable -PathType Leaf)) { throw "빌드가 없습니다: $executable" }
$localAppDataParent = Split-Path ([Environment]::GetFolderPath('LocalApplicationData')) -Parent
$outputDirectory = Join-Path $localAppDataParent 'LocalLow/DefaultCompany/Zombie Shoot VR/Diagnostics'
if ($CheckOnly) {
    [pscustomobject]@{ Executable = $executable; Argument = '-vrfps-diagnostics'; OutputDirectory = $outputDirectory }
    return
}
$process = Start-Process -FilePath $executable -WorkingDirectory (Split-Path $executable -Parent) -ArgumentList '-vrfps-diagnostics' -WindowStyle Hidden -PassThru
[pscustomobject]@{ ProcessId = $process.Id; OutputDirectory = $outputDirectory }
