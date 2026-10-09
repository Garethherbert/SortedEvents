[CmdletBinding()]
param([ValidateSet('BigDaySorted','LiveDaysSorted','Both')][string]$Brand='Both',[int]$BuildNumber=5,[string]$AndroidKeystore='',[string]$AndroidAlias='',[switch]$RestoreWorkloads)
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
& npm ci --ignore-scripts
if($LASTEXITCODE){throw 'Website dependency installation failed.'}
& npm run build
if($LASTEXITCODE){throw 'Website build failed.'}
& npm test
if($LASTEXITCODE){throw 'Backend tests failed.'}
$brands=if($Brand -eq 'Both'){@('BigDaySorted','LiveDaysSorted')}else{@($Brand)}
foreach($product in $brands){
& "$PSScriptRoot/Build-Windows.ps1" -Brand $product -BuildNumber $BuildNumber -RestoreWorkloads:$RestoreWorkloads
if($AndroidKeystore){& "$PSScriptRoot/Build-Android.ps1" -Format aab -Brand $product -BuildNumber $BuildNumber -Keystore $AndroidKeystore -Alias $AndroidAlias -RestoreWorkloads:$RestoreWorkloads}else{& "$PSScriptRoot/Build-Android.ps1" -Format apk -Brand $product -BuildNumber $BuildNumber -RestoreWorkloads:$RestoreWorkloads}
}
Write-Host 'Website build, Windows package and Android package complete. Apple builds run separately on macOS.'
