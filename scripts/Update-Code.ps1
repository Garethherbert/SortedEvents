[CmdletBinding()]
param([string]$Branch='main')
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if($Branch -notmatch '^[A-Za-z0-9][A-Za-z0-9/_.-]*$'){throw 'Invalid branch name.'}
$changes=& git status --porcelain
if($LASTEXITCODE){throw 'This is not a Git checkout. Read START-HERE.md first.'}
if($changes){throw 'Commit or resolve local source changes before updating. No changes were discarded.'}
& git fetch origin
if($LASTEXITCODE){throw 'Git fetch failed.'}
& git switch $Branch
if($LASTEXITCODE){throw 'Could not select branch.'}
& git pull --ff-only origin $Branch
if($LASTEXITCODE){throw 'Git update failed; resolve divergent branches manually.'}
