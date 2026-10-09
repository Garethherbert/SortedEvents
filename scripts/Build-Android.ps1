[CmdletBinding()]
param([ValidateSet('BigDaySorted','LiveDaysSorted')][string]$Brand='BigDaySorted',[ValidateSet('apk','aab')][string]$Format='apk',[int]$BuildNumber=5,[string]$Keystore='',[string]$Alias='',[switch]$RestoreWorkloads)
$ErrorActionPreference='Stop'
if(-not $Alias){$Alias=$Brand.ToLower()+'-upload'}
Set-Location (Split-Path $PSScriptRoot -Parent)
$stage=& node "$PSScriptRoot/prepare-native-brand.mjs" $Brand
if($LASTEXITCODE){throw 'Native source preparation failed.'}
$project=Join-Path $stage 'src/BigDaySorted.App/BigDaySorted.App.csproj' ;$tfm='net10.0-android'
if(-not (Get-Command dotnet -ErrorAction SilentlyContinue)){throw 'Install .NET 10 and the MAUI Android workload first.'}
if($RestoreWorkloads){dotnet workload restore $project "-p:BigDaySortedTargetFramework=$tfm" "-p:AppBrand=$Brand";if($LASTEXITCODE){throw 'Workload restore failed.'}}
$argsList=@('publish',$project,'-f',$tfm,'-c','Release',"-p:BigDaySortedTargetFramework=$tfm", "-p:AppBrand=$Brand","-p:AndroidPackageFormats=$Format","-p:ApplicationVersion=$BuildNumber",'-o',"artifacts/$Brand/android/build-$BuildNumber")
if($Keystore){if(-not(Test-Path $Keystore)){throw 'Keystore not found.'};$Keystore=(Resolve-Path $Keystore).Path;if(-not $env:BDS_KEYSTORE_PASSWORD -or -not $env:BDS_KEY_PASSWORD){throw 'Set BDS_KEYSTORE_PASSWORD and BDS_KEY_PASSWORD in your environment. Do not commit them.'};$argsList+=@('-p:AndroidKeyStore=true',"-p:AndroidSigningKeyStore=$Keystore","-p:AndroidSigningKeyAlias=$Alias",'-p:AndroidSigningStorePass=env:BDS_KEYSTORE_PASSWORD','-p:AndroidSigningKeyPass=env:BDS_KEY_PASSWORD')}elseif($Format -eq 'aab'){throw 'A Play upload requires -Keystore and signing passwords.'}
& dotnet @argsList
if($LASTEXITCODE){throw 'Android build failed.'}
Write-Host "Android output: artifacts/$Brand/android/build-$BuildNumber"
