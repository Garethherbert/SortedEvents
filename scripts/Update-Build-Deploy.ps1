[CmdletBinding()]
param([string]$Branch='main',[ValidateSet('BigDaySorted','LiveDaysSorted','Both')][string]$Brand='Both',[int]$BuildNumber=5,[switch]$SkipPull,[switch]$SkipCloudflare,[switch]$SkipWindows,[switch]$SkipAndroid,[switch]$RestoreWorkloads)
$ErrorActionPreference='Stop'
if(-not $SkipPull){& "$PSScriptRoot/Update-Code.ps1" -Branch $Branch}
if(-not $SkipCloudflare){& "$PSScriptRoot/Deploy-Cloudflare.ps1"}
if(-not($SkipWindows -and $SkipAndroid)){& "$PSScriptRoot/Build-NativeApps.ps1" -Brand $Brand -BuildNumber $BuildNumber -SkipWindows:$SkipWindows -SkipAndroid:$SkipAndroid -RestoreWorkloads:$RestoreWorkloads}
