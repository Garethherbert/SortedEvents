# BigDaySorted and LiveDaysSorted

**Big day? Sorted.** Celebrations under BigDaySorted; concerts and festivals under LiveDaysSorted. Shared website and C# source, with separate app identities.

For the current two-brand native build commands, read [docs/NATIVE-APPS.md](docs/NATIVE-APPS.md).

## Independent GitHub and Cloudflare deployment

Start with [START-HERE.md](START-HERE.md). It includes repository creation, domain configuration, verified Cloudflare Access organiser sign-in, separate D1/R2 resources, deployment and manual-distribution native build commands. The websites and API run together in one Worker per brand.

## What this delivery contains

- A marketing website and working browser organiser.
- A persistent event API using D1 and moderated file storage using R2.
- A shared .NET 10 MAUI client project targeting Windows, Android, Mac Catalyst and iOS.
- Reused KeepMyTravels core models, SQLite repository, booking parser and secure Wi-Fi transfer engine.
- Tests for role permissions, secret gift choices, reservation capacity, invitation revocation, concurrent updates and gallery moderation.

**This is an initial testing edition, not a finished commercial release.** Native binaries were not built in the Linux authoring environment. The MAUI source needs Windows/macOS builds and device testing. The earlier ChatGPT-hosted preview is private. This standalone export adds Cloudflare deployment and independent organiser authentication; configure and test them using START-HERE.md before an external pilot.

## Try the website locally

Install Node 24 or newer:

```sh
npm ci --ignore-scripts
npm run build
npm test
npm start
```

Open http://localhost:3000. This server binds only to loopback and supplies a local test organiser identity. Do not expose it on the internet. Data persists in `.local/events.sqlite`; uploaded bytes persist in `.local/media`. Hosted deployments use D1 and R2 instead.

## Native builds

Install .NET 10, the applicable MAUI workloads, Android SDK/JDK and, for Apple platforms, compatible Xcode. Use:

```powershell
.\scripts\Build-WindowsInstaller.ps1 -BuildNumber 2 -RestoreWorkloads
.\scripts\Build-Android.ps1 -Format apk -BuildNumber 2 -RestoreWorkloads
```

For a signed Play bundle:

```powershell
# Set BDS_KEYSTORE_PASSWORD and BDS_KEY_PASSWORD securely in this shell.
.\scripts\Build-Android.ps1 -Format aab -BuildNumber 2 -Keystore 'C:\signing\bigdaysorted-upload.jks'
```

On a Mac:

```sh
# Set BDS_CODESIGN_KEY and, where required, BDS_PROVISION_PROFILE.
./scripts/Build-Apple.sh mac
./scripts/Build-Apple.sh ios
./scripts/Build-Apple.sh simulator
```

Windows installer packaging uses Inno Setup 6. Store identity reservations, signing certificates and provisioning profiles are not included. Apple builds require a Mac; no IPA, PKG, AAB, APK, EXE or MSIX was produced in this delivery.

## Local/native and shared plans

Native clients save local plans offline with the reused KeepMyTravels SQLite repository plus an event-specific sidecar. The browser uses the persistent event API. These are separate stores in this edition: export/import `.bigday` records to move a plan between them. Automatic native cloud synchronisation is not yet implemented.

Online RSVPs, invitations, gifts, gallery uploads and thank-you drafts are implemented in the browser workspace and opened by the native client. There is no paid checkout, entitlement enforcement or automatic email sending. Proposed prices on the website do not grant licences.

## Collaboration

The organiser owns the event. Named helpers can update only work assigned to their exact invitation name. Guests can RSVP, reserve gifts and upload photos for moderation. Honourees receive guest-level viewing and cannot see gift reservations early. Gallery-only links show approved gallery content. Invitation secrets are returned once, hashed in storage and individually revocable.

Organisers/honourees cannot retrieve purchaser identities or reservation totals before reveal. Gift pledges are stored separately from event plans and excluded from exports. A gift reservation is a guest's stated intent, not proof of an external purchase.

## Gift reveal

Reveal time is no earlier than the end of the final event day across supported time zones, with a conservative 14-hour UTC padding. It can therefore unlock later than local midnight. Changing the event earlier cannot reduce the original protected reveal; moving it later extends protection. This prevents date editing from exposing gifts early. Retailers' own websites may reveal their purchase information independently.

## Uploaded files

JPEG and PNG gallery photos; PDF organiser documents. Max 10 MB per file and a testing quota of 100 MB per event. Guest photos require organiser approval. Video support, transcoding and large production storage quotas remain future work.

## Gift registry connections

Link out to Amazon or any HTTPS registry/product URL. Guest reservations, self-reported purchases and post-event gift-linked thank-you drafts work within BigDaySorted. Automatic Amazon registry import, purchase webhooks and fulfilment are not implemented; no approved registry purchase-history API was verified. See docs/RESEARCH-AND-PRICING.md.

## Hosting

`.openai/hosting.json` identifies the private BigDaySorted Site, independently from KeepMyTravels. Worker output is built with `npm run build`; Drizzle migrations live in `drizzle`. Sites owns deployment, D1 and R2 resource configuration. Source includes the complete native projects so the product remains reviewable in one repository.

KeepMyTravels source provenance and exact reused files: `docs/REUSE-MANIFEST.json`.

## UI update — 0.1.1 build 2

The Windows/mobile source now uses a consistent light theme, 16px form text, permanent field labels, visible scrollbars, Back buttons and an always-accessible Menu. Wide windows show a sidebar; narrow screens can toggle it. Event creation asks concert/festival days, stages and individual daily band totals; weddings/other events ask expected attendance. Festival/concert workspace menus omit seating and gifts. A daily duration-based critical path highlights zero-float dependency chains. Drag cards or use Move up/down to resequence a selected day/area; timings become consecutive from the first time, while dependencies are retained and timing warnings reported. Local/native editing still requires a new build.

For a Windows testing installer, extract the updated complete source, open PowerShell in that folder, and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build-WindowsInstaller.ps1 -BuildNumber 2 -RestoreWorkloads
```

Output: `artifacts/installers/BigDaySorted-0.1.1-build2-Setup.exe`. The .NET 10 MAUI workload and Inno Setup 6 are required. Native source was not compiled here; installation, Windows drag/drop and small-screen layout need device testing. Existing local event files remain in the same application data directory. New setup questions create templates for new events; editing setup values does not overwrite an existing customised running order.
