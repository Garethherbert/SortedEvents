[CmdletBinding()]
param([ValidateSet('BigDaySorted','LiveDaysSorted')][string]$Brand='BigDaySorted',[ValidateSet('Debug','Release')][string]$Configuration='Release',[int]$BuildNumber=5,[switch]$CreateMsix,[string]$CertificatePath='',[switch]$RestoreWorkloads)
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if(-not $IsWindows -and $PSVersionTable.PSEdition -eq 'Core'){throw 'Run this on Windows.'}
if(-not (Get-Command dotnet -ErrorAction SilentlyContinue)){throw 'Install the .NET 10 SDK and Visual Studio .NET MAUI workload first.'}
$stage=& node "$PSScriptRoot/prepare-native-brand.mjs" $Brand
if($LASTEXITCODE){throw 'Native source preparation failed.'}
$project=Join-Path $stage 'src/BigDaySorted.App/BigDaySorted.App.csproj' 
$tfm='net10.0-windows10.0.19041.0'
if($RestoreWorkloads){dotnet workload restore $project "-p:BigDaySortedTargetFramework=$tfm" "-p:AppBrand=$Brand";if($LASTEXITCODE){throw 'Workload restore failed.'}}
$argsList=@('publish',$project,'-f',$tfm,'-c',$Configuration,"-p:BigDaySortedTargetFramework=$tfm", "-p:AppBrand=$Brand",'-p:RuntimeIdentifierOverride=win-x64',"-p:ApplicationVersion=$BuildNumber",'-p:WindowsAppSDKSelfContained=true','-p:SelfContained=true','-o',"artifacts/$Brand/windows/build-$BuildNumber")
if($CreateMsix){if(-not (Test-Path $CertificatePath)){throw 'Supply -CertificatePath with a valid signing certificate (.pfx).'};$CertificatePath=(Resolve-Path $CertificatePath).Path;$argsList+=@('-p:WindowsPackageType=MSIX','-p:GenerateAppxPackageOnBuild=true',"-p:PackageCertificateKeyFile=$CertificatePath",'-p:AppxPackageSigningEnabled=true')}else{$argsList+='-p:WindowsPackageType=None'}
& dotnet @argsList
if($LASTEXITCODE){throw 'Windows build failed.'}
Write-Host "Windows output: artifacts/$Brand/windows/build-$BuildNumber"
Write-Host 'This source is an initial testing edition. Test the output before distributing it.'
