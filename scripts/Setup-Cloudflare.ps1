[CmdletBinding()]
param([string]$AccountId='', [string[]]$BigDayDomains=@(), [string[]]$LiveDayDomains=@(), [string]$AccessTeamDomain='', [string]$BigDayAccessAud='', [string]$LiveDayAccessAud='', [switch]$Provision)
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if(-not $AccountId){$AccountId=Read-Host 'Cloudflare account ID'}
if(-not $BigDayDomains.Count){$BigDayDomains=(Read-Host 'BigDaySorted domains, primary first, comma-separated').Split(',') | ForEach-Object {$_.Trim().ToLower()}}
if(-not $LiveDayDomains.Count){$LiveDayDomains=(Read-Host 'LiveDaysSorted domains, primary first, comma-separated').Split(',') | ForEach-Object {$_.Trim().ToLower()}}
if(-not $AccessTeamDomain){$AccessTeamDomain=Read-Host 'Access team URL (https://YOUR-TEAM.cloudflareaccess.com)'}
if(-not $BigDayAccessAud){$BigDayAccessAud=Read-Host 'BigDaySorted Access application AUD'}
if(-not $LiveDayAccessAud){$LiveDayAccessAud=Read-Host 'LiveDaysSorted Access application AUD'}
$config=@{accountId=$AccountId;accessTeamDomain=$AccessTeamDomain.TrimEnd('/');products=@{BigDaySorted=@{domains=@($BigDayDomains);accessAud=$BigDayAccessAud;databaseName='bigdaysorted-events';bucketName='bigdaysorted-media'};LiveDaysSorted=@{domains=@($LiveDayDomains);accessAud=$LiveDayAccessAud;databaseName='livedayssorted-events';bucketName='livedayssorted-media'}}}
$config | ConvertTo-Json -Depth 6 | & node "$PSScriptRoot/configure-cloudflare.mjs"
if($LASTEXITCODE){throw 'Configuration failed.'}
& npm ci --ignore-scripts
if($LASTEXITCODE){throw 'Dependency installation failed.'}
if($Provision){& node "$PSScriptRoot/provision-cloudflare.mjs";if($LASTEXITCODE){throw 'Cloudflare provisioning failed.'}}
Write-Host 'Configuration complete. See START-HERE.md for sign-in and deployment steps.'
