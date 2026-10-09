# Sorted Events: GitHub, Cloudflare and native app builds

This single repository contains BigDaySorted and LiveDaysSorted: the websites, browser organiser, API, database migrations, shared .NET MAUI Windows/Android/iOS/Mac source, reused KeepMyTravels libraries, tests and build scripts. It is a testing edition. Billing, plan enforcement, automatic email delivery and native cloud sync are not complete. Native apps currently open the online workspace for collaborative features.

## 1. Create your GitHub repository

Create an empty PRIVATE repository in GitHub (do not initialise it with a README). Extract this ZIP into `C:\dev\SortedEvents`, so `package.json` and `START-HERE.md` are directly in that folder. In PowerShell:

```powershell
Set-Location C:\dev\SortedEvents
git init -b main
git add .
git commit -m "Initial BigDaySorted and LiveDaysSorted source"
git remote add origin https://github.com/YOUR-USERNAME/YOUR-REPOSITORY.git
git push -u origin main
```

For another computer:

```powershell
git clone https://github.com/YOUR-USERNAME/YOUR-REPOSITORY.git C:\dev\SortedEvents
Set-Location C:\dev\SortedEvents
```

The ZIP contains no Git credentials, API tokens, signing keys, databases, build output or node_modules. Deployment configuration and build output are gitignored. Store your Android keystores and Apple/Windows certificates outside this repository.

## 2. Install tools

On Windows: Git, Node.js 24+, .NET 10 SDK, Visual Studio's .NET MAUI tooling, the Windows SDK, the MAUI Android workload, supported Android SDK/JDK and Inno Setup 6 for Setup.exe packaging. Scripts work in Windows PowerShell or PowerShell 7; if script execution is blocked, use `Set-ExecutionPolicy -Scope Process Bypass` for this terminal session.

On Mac: Git, Node.js 24+, .NET 10 with MAUI iOS/Mac Catalyst workloads, supported Xcode and your Apple signing identities/profiles. iOS and Mac builds must run on a Mac. Signing and store submission remain manual.

## 3. Set up Cloudflare organiser sign-in

Registering domains does not create hosting, storage or sign-in. Set up Cloudflare Zero Trust with an identity provider (email one-time PIN is suitable for private testing). Create two self-hosted Access applications:

- BigDaySorted: each of its configured hostnames, with path `/organiser-login`.
- LiveDaysSorted: each of its configured hostnames, with path `/organiser-login`.

Add an Allow policy for your organiser accounts. Copy each application's AUD tag and your `https://YOUR-TEAM.cloudflareaccess.com` team URL. In each application's cookie settings enable HttpOnly and leave Cookie Path disabled so the application cookie is sent to `/api/` too. Do not protect the entire domain: the landing page and personal guest/crew links must stay reachable. The Worker validates the JWT signature, issuer, audience and expiry itself and discards spoofed identity headers.

Organisers go through `/organiser-login`; on success they reach `/app.html`. Guests and helpers use their individual `/app.html#invite=...` links, without needing an organiser licence or Access account. Their existing API permissions still apply. A guessed/expired/revoked link does not grant access. External testing should verify this complete login flow after deployment.

Cloudflare Access is the testing organiser sign-in provider, not a subscription/billing system. Check its current organiser-seat costs and limits before a broad launch.

## 4. Configure your actual domains and resources

Create a Cloudflare API token scoped to the correct account/zones with D1 Read/Write, Workers R2 Storage Read/Write, Workers Scripts Edit, and permissions required to publish Workers Custom Domains (Workers Routes Edit, Zone Read and DNS Edit for your zones). Enable R2 on the account first. Copy the token to the clipboard, then set it for the current terminal without writing it into the repo:

```powershell
$env:CLOUDFLARE_API_TOKEN = (Get-Clipboard).Trim()
.\scripts\Setup-Cloudflare.ps1 -Provision
```

The script prompts for your account ID, both brands' exact domains (comma-separated, primary first), Access team URL and the two AUD values. It creates/reuses `bigdaysorted-events`, `livedayssorted-events`, `bigdaysorted-media` and `livedayssorted-media`. Media buckets stay private; the API controls file access. Your domain extensions are not assumed. Add www and other aliases only when you want this Worker to own them and they belong to your account's active zones. Existing DNS/Worker ownership conflicts must be resolved in Cloudflare before deployment.

Configuration is written to `cloudflare/deploy.config.json` (gitignored). It contains no secrets. Keep a backup and copy it to other build machines so native apps use the same domains. `cloudflare/deploy.config.example.json` shows the schema. If you already have D1/R2 resources, set their IDs/names in the local config rather than provision replacements; do not point this testing code at unrelated databases.

## 5. Build and deploy websites and APIs

```powershell
# Compile and package only; no remote migrations/deploy:
.\scripts\Deploy-Cloudflare.ps1 -DryRun

# Run checks, apply migrations and deploy both brands:
.\scripts\Deploy-Cloudflare.ps1
```

There is one Worker per brand. Each serves the marketing website, organiser and `/api/` endpoints on the same domain, avoiding a separate API domain and CORS setup. Wrangler Custom Domains creates DNS/certificates for configured hostnames. workers.dev and preview URLs are disabled. There is no automatic deployment of the existing ChatGPT-hosted previews.

Both databases are separate. Only unapplied migrations are executed. Take a D1 backup before subsequent schema changes. Deployments are sequential; if the second brand fails, the first may already have deployed. Fix the reported failure and rerun. Provisioning retries reuse resources by name.

Check `/health` on both primary domains; then test organiser login, event creation, a guest/crew link in a signed-out browser, gallery uploads and event isolation. This authoring environment verified bundles and automated tests, but did not deploy into your Cloudflare account or test your real Access configuration.

## 6. Build Windows installers and Android apps

```powershell
# Both brands: single-file Windows installers plus testing Android APKs.
# Use your next unused build number for each release.
.\scripts\Build-NativeApps.ps1 -Brand Both -BuildNumber 6 -RestoreWorkloads

# Windows only:
.\scripts\Build-NativeApps.ps1 -Brand Both -BuildNumber 6 -SkipAndroid

# Android only (APK for testing):
.\scripts\Build-NativeApps.ps1 -Brand Both -BuildNumber 6 -SkipWindows
```

Windows installers: `artifacts/installers/<brand>-0.1.4-build6-Setup.exe`. Published Windows app files: `artifacts/<brand>/windows/build-6`. Android output: `artifacts/<brand>/android/build-6`. Sign Windows installers with your own code-signing certificate before public distribution. The separate Build-Windows.ps1 also supports signed MSIX builds; its certificate publisher must match the selected platform manifest.

For Play upload AABs, build each brand separately with its own keystore and passwords:

```powershell
$env:BDS_KEYSTORE_PASSWORD = (Read-Host 'Store password')
$env:BDS_KEY_PASSWORD = (Read-Host 'Key password')
.\scripts\Build-Android.ps1 -Brand BigDaySorted -Format aab -BuildNumber 6 -Keystore C:\Signing\bigdaysorted-upload.jks -Alias bigdaysorted-upload -RestoreWorkloads
# Set the passwords for the second keystore before this command:
.\scripts\Build-Android.ps1 -Brand LiveDaysSorted -Format aab -BuildNumber 6 -Keystore C:\Signing\livedayssorted-upload.jks -Alias livedayssorted-upload -RestoreWorkloads
```

Passwords are environment variables, not literal command arguments. Use a private terminal. No script submits builds to Google Play or Microsoft Store. Build number must exceed the last uploaded number. Application IDs: `com.keepmytravels.bigdaysorted` and `com.keepmytravels.livedayssorted`. Windows store publisher identities need setting to the values from your own store accounts.

## 7. Mac and iOS builds (on your Mac)

Copy `cloudflare/deploy.config.json` to this clone first. Each brand needs its own matching Apple profile; the shared BDS_ variable names are just the script interface.

```bash
export BDS_BUILD_NUMBER=6
export BDS_CODESIGN_KEY='YOUR APPLE SIGNING IDENTITY'
export BDS_PROVISION_PROFILE='PROFILE FOR SELECTED BRAND'
bash scripts/Build-Apple.sh ios BigDaySorted
# Set the matching profile for LiveDaysSorted:
bash scripts/Build-Apple.sh ios LiveDaysSorted
# Mac defaults to Apple Silicon; Intel: export BDS_APPLE_RID=maccatalyst-x64
bash scripts/Build-Apple.sh mac BigDaySorted
bash scripts/Build-Apple.sh mac LiveDaysSorted
# No release signing needed for the simulator target:
bash scripts/Build-Apple.sh simulator BigDaySorted
```

Outputs are under `artifacts/<brand>/<platform>/build-6`. Release iOS builds request an archive and IPA; Mac builds request a package. Apple signing, distribution/notarisation requirements and submission are your manual steps. No native compilation or installer/device tests were possible in this Linux environment.

## 8. Routine updates

```powershell
# Pull only (stops if source changes are uncommitted):
.\scripts\Update-Code.ps1

# Pull, check, deploy both APIs/websites, build Windows installers and Android APKs:
.\scripts\Update-Build-Deploy.ps1 -BuildNumber 7 -RestoreWorkloads

# Just pull and deploy the cloud parts:
.\scripts\Update-Build-Deploy.ps1 -SkipWindows -SkipAndroid

# Pull and build native apps without redeploying websites:
.\scripts\Update-Build-Deploy.ps1 -SkipCloudflare -BuildNumber 7
```

These scripts never stash or discard local source changes; pulls use fast-forward only. Use `-Branch` if your repository's branch is not main. After source edits, commit and push them yourself before using update scripts.

On Mac: `bash scripts/Update-Build-Apple.sh ios BigDaySorted main` pulls and builds one selected Apple target. Set signing variables first. Existing scripts/Build-Everything.ps1 is retained, but use the new scripts above for your independent Cloudflare setup.

## Reference documentation

- https://developers.cloudflare.com/workers/wrangler/configuration/
- https://developers.cloudflare.com/d1/reference/migrations/
- https://developers.cloudflare.com/api/resources/d1/subresources/database/methods/create/
- https://developers.cloudflare.com/api/resources/r2/subresources/buckets/methods/create/
- https://developers.cloudflare.com/cloudflare-one/access-controls/applications/http-apps/authorization-cookie/
- https://developers.cloudflare.com/cloudflare-one/access-controls/applications/http-apps/authorization-cookie/validating-json/
