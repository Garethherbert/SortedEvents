[CmdletBinding()]
param([ValidateSet('BigDaySorted','LiveDaysSorted')][string]$Brand='BigDaySorted',[int]$BuildNumber=5,[switch]$RestoreWorkloads)
$ErrorActionPreference='Stop'
& "$PSScriptRoot/Build-Windows.ps1" -Brand $Brand -BuildNumber $BuildNumber -RestoreWorkloads:$RestoreWorkloads
$compiler=Get-Command ISCC.exe -ErrorAction SilentlyContinue
if($compiler){$iscc=$compiler.Source}else{$iscc=Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'}
if(-not(Test-Path $iscc)){throw 'Windows app was built. Install Inno Setup 6, then rerun this script to create a single-file installer.'}
$publish=(Resolve-Path (Join-Path $PSScriptRoot "../artifacts/$Brand/windows/build-$BuildNumber")).Path
& $iscc "/DPublishDir=$publish" "/DAppName=$Brand" "/DBuildNumber=$BuildNumber" "$PSScriptRoot/BigDaySorted-Setup.iss"
if($LASTEXITCODE){throw 'Installer packaging failed.'}
Write-Host "Installer: artifacts/installers/${Brand}-0.1.4-build$BuildNumber-Setup.exe"
Write-Host 'Sign the installer with your own code-signing certificate before public distribution.'
