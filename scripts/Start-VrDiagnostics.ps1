param(
    [ValidateSet('MenuLab', 'GamgoMenuLab', 'MixamoMenuLab', 'HitReactionMenuLab', 'FeedbackMenuLab', 'ClothingFixMenuLab', 'UserAudioMenuLab', 'BossCrawlerMenuLab', 'CrawlerDeathMenuLab', 'RoomScan', 'AbandonedHouse', 'AbandonedSchool', 'WallFix', 'WindowsDemo', 'BootstrapPreview')]
    [string]$Build = 'WallFix',
    [switch]$CheckOnly,
    [switch]$FrameTimings
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$executableName = if ($Build -eq 'BootstrapPreview') { 'BootstrapPreview.exe' } elseif ($Build -eq 'RoomScan') { 'RoomScan.exe' } elseif ($Build -eq 'AbandonedHouse') { 'AbandonedHouse.exe' } elseif ($Build -in @('AbandonedSchool', 'WallFix')) { 'AbandonedSchool.exe' } else { 'WaveLab.exe' }
$executable = Join-Path $workspace "Zombie Shoot VR/Builds/Windows/$Build/$executableName"
$namedPlayer = Join-Path (Split-Path $executable -Parent) 'ZomV.exe'
if (Test-Path -LiteralPath $namedPlayer) { $executable = $namedPlayer }
if (!(Test-Path -LiteralPath $executable -PathType Leaf)) { throw "빌드가 없습니다: $executable" }
$localAppDataParent = Split-Path ([Environment]::GetFolderPath('LocalApplicationData')) -Parent
$productName = (Get-Content -LiteralPath (Join-Path (Split-Path $executable -Parent) (([IO.Path]::GetFileNameWithoutExtension($executable)) + '_Data/app.info')))[1]
$outputDirectory = Join-Path $localAppDataParent "LocalLow/DefaultCompany/$productName/Diagnostics"
$captureArguments = if ($FrameTimings) { '-vrfps-diagnostics -vrfps-frame-timings' } else { '-vrfps-diagnostics' }
if ($CheckOnly) {
    [pscustomobject]@{ Executable = $executable; Argument = $captureArguments; OutputDirectory = $outputDirectory }
    return
}
$process = Start-Process -FilePath $executable -WorkingDirectory (Split-Path $executable -Parent) -ArgumentList $captureArguments -WindowStyle Hidden -PassThru
[pscustomobject]@{ ProcessId = $process.Id; OutputDirectory = $outputDirectory }
