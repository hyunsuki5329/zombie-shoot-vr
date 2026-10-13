param(
    [ValidateSet('MenuLab', 'GamgoMenuLab', 'MixamoMenuLab', 'HitReactionMenuLab', 'FeedbackMenuLab', 'ClothingFixMenuLab', 'UserAudioMenuLab', 'BossCrawlerMenuLab', 'CrawlerDeathMenuLab', 'RoomScan', 'AbandonedHouse', 'BootstrapPreview')]
    [string]$Build = 'MenuLab',
    [switch]$CheckOnly,
    [switch]$FrameTimings
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$executableName = if ($Build -eq 'BootstrapPreview') { 'BootstrapPreview.exe' } elseif ($Build -eq 'RoomScan') { 'RoomScan.exe' } elseif ($Build -eq 'AbandonedHouse') { 'AbandonedHouse.exe' } else { 'WaveLab.exe' }
$executable = Join-Path $workspace "Zombie Shoot VR/Builds/Windows/$Build/$executableName"
if (!(Test-Path -LiteralPath $executable -PathType Leaf)) { throw "빌드가 없습니다: $executable" }
$localAppDataParent = Split-Path ([Environment]::GetFolderPath('LocalApplicationData')) -Parent
$outputDirectory = Join-Path $localAppDataParent 'LocalLow/DefaultCompany/Zombie Shoot VR/Diagnostics'
$captureArguments = if ($FrameTimings) { '-vrfps-diagnostics -vrfps-frame-timings' } else { '-vrfps-diagnostics' }
if ($CheckOnly) {
    [pscustomobject]@{ Executable = $executable; Argument = $captureArguments; OutputDirectory = $outputDirectory }
    return
}
$process = Start-Process -FilePath $executable -WorkingDirectory (Split-Path $executable -Parent) -ArgumentList $captureArguments -WindowStyle Hidden -PassThru
[pscustomobject]@{ ProcessId = $process.Id; OutputDirectory = $outputDirectory }
