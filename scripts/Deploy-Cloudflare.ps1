[CmdletBinding()]
param([switch]$DryRun,[switch]$Provision)
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
& npm ci --ignore-scripts
if($LASTEXITCODE){throw 'Dependency installation failed.'}
if($Provision){& node scripts/provision-cloudflare.mjs;if($LASTEXITCODE){throw 'Provisioning failed.'}}
& npm run build
if($LASTEXITCODE){throw 'Website build failed.'}
& npm test
if($LASTEXITCODE){throw 'Automated checks failed; deployment stopped.'}
& npm run cloud:build
if($LASTEXITCODE){throw 'Cloudflare build failed.'}
if($DryRun){& npm run cloud:dry-run}else{& npm run cloud:deploy}
if($LASTEXITCODE){throw 'Cloudflare deployment failed.'}
