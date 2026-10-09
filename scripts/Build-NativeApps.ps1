[CmdletBinding()]
param([ValidateSet('BigDaySorted','LiveDaysSorted','Both')][string]$Brand='Both',[int]$BuildNumber=5,[ValidateSet('apk','aab')][string]$AndroidFormat='apk',[string]$BigDayKeystore='',[string]$LiveDayKeystore='',[switch]$RestoreWorkloads,[switch]$SkipWindows,[switch]$SkipAndroid)
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if(-not(Test-Path 'cloudflare/deploy.config.json')){throw 'Configure your domains using Setup-Cloudflare.ps1 before native builds.'}
$products=if($Brand -eq 'Both'){@('BigDaySorted','LiveDaysSorted')}else{@($Brand)}
foreach($product in $products){
 if(-not $SkipWindows){& "$PSScriptRoot/Build-WindowsInstaller.ps1" -Brand $product -BuildNumber $BuildNumber -RestoreWorkloads:$RestoreWorkloads}
 if(-not $SkipAndroid){$keystore=if($product -eq 'BigDaySorted'){$BigDayKeystore}else{$LiveDayKeystore}; & "$PSScriptRoot/Build-Android.ps1" -Brand $product -Format $AndroidFormat -BuildNumber $BuildNumber -Keystore $keystore -RestoreWorkloads:$RestoreWorkloads}
}
Write-Host 'Native output is under artifacts/. No apps were uploaded or submitted.'
