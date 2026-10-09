# BigDaySorted and LiveDaysSorted native apps — 0.1.4, build 5

Both brands share the MAUI app and event engine. Class namespaces and source project filenames remain BigDaySorted to avoid maintaining duplicate code. ProductBrand selects the displayed name, event types, colours, workspace URL, transfer scheme and local storage folder. KeepMyTravels Core and Infrastructure source remains reused.

BigDaySorted creates celebrations. Its Day plan retains granular timings, checklists, responsibilities and daily critical paths, without band/stage menu or artist/soundcheck fields. Invitations, seating, gifts and thank-you workflows are in its online workspace. LiveDaysSorted creates concerts and festivals with Bands & stages, Running order, multi-day templates and critical paths. Its online workspace has Crew access rather than guest invitations, seating, gifts or thank-you notes. Gallery and document tools are shared. Native collaboration/gallery features open the online workspace; they are not an offline copy of the website's data.

## Build prerequisites

Node.js 24, .NET 10 SDK and the relevant MAUI workloads. Windows requires the Windows SDK/Visual Studio MAUI tooling; Android requires its supported JDK and SDK. Apple builds require macOS, Xcode and appropriate signing identities/profiles. Install Inno Setup 6 to produce a Windows Setup.exe. No signing keys are included.

Build scripts prepare isolated copies under .native-build/<brand>, with that brand's platform manifests, icon, splash and C# build symbol. Generated copies are disposable, not separate maintained source. Do not run two builds for the same brand simultaneously, because preparation resets its staging folder. Source projects can build BigDaySorted directly; LiveDaysSorted must use the preparation script to ensure all platform identities are correct.

```powershell
./scripts/Build-WindowsInstaller.ps1 -Brand BigDaySorted -RestoreWorkloads
./scripts/Build-WindowsInstaller.ps1 -Brand LiveDaysSorted -RestoreWorkloads
./scripts/Build-Android.ps1 -Brand BigDaySorted -Format apk -RestoreWorkloads
./scripts/Build-Android.ps1 -Brand LiveDaysSorted -Format apk -RestoreWorkloads
# Both Windows published apps plus Android APKs (no Setup.exe packaging):
./scripts/Build-Everything.ps1 -Brand Both -RestoreWorkloads
```

```bash
bash scripts/Build-Apple.sh mac BigDaySorted
bash scripts/Build-Apple.sh mac LiveDaysSorted
bash scripts/Build-Apple.sh ios BigDaySorted
bash scripts/Build-Apple.sh ios LiveDaysSorted
bash scripts/Build-Apple.sh simulator LiveDaysSorted
```

Windows/Android outputs: artifacts/<brand>/<platform>/build-5. Windows installers: artifacts/installers/<brand>-0.1.4-build5-Setup.exe. Apple output uses the brand and build number too. Application IDs are com.keepmytravels.bigdaysorted and com.keepmytravels.livedayssorted; executable names and installer GUIDs differ so the apps can coexist. Existing BigDaySorted local folder and installer GUID are retained.

For signed Android AABs, pass -Keystore and -Alias. Default aliases are bigdaysorted-upload and livedayssorted-upload. Passwords use BDS_KEYSTORE_PASSWORD and BDS_KEY_PASSWORD environment variables for either selected build; set the correct values per keystore. For Apple builds, set BDS_CODESIGN_KEY and BDS_PROVISION_PROFILE to the identity/profile for that selected brand; BDS_APPLE_RID optionally selects the Mac architecture and BDS_BUILD_NUMBER overrides build 5. The shared environment variable prefix is kept for compatibility, not shared provisioning: each bundle ID needs a matching profile. Windows MSIX uses -CreateMsix -CertificatePath with a publisher matching the selected manifest; store release publishers still need configuring.

## Transfers and old plans

.bigday stays version 1 with format identifier BigDaySorted for both products. Both apps can manually import either product's planning file or transfer link, preserving event type and timeline dependencies. New-event choices are brand-specific; an imported legacy event is not silently converted or deleted. Deep links register only their own product scheme. Online workspace imports are subject to its event-type restrictions. Photos, invitations, gift pledges and private cloud data are not included in these archives. Automatic cloud synchronisation is not implemented.

## Graph critical path

Daily critical path defaults to an offline dependency graph, with amber critical nodes/edges, duration, clock time, responsible person and area. Day/area filters retain prerequisite activities from other areas as dashed context cards. Move handles support mouse/touch dragging; corner handles resize cards. Pointer and keyboard interaction code is shared with the website. Layouts save to local event data and .bigday exports; arrows follow card positions. These layout changes do not alter times, durations or dependencies. A zoom slider controls scale; scroll within the graph. Switch to the detailed list for timing edits and drag/up-down rescheduling. The whole-day critical calculation stays unchanged.

## Validation and release status

JavaScript/API/UI regression tests and brand preparation checks run on Linux. Platform manifests, Apple plists, SVGs and project XML are checked structurally; shell syntax is checked. This environment has no .NET SDK, Windows or Xcode, so C# compilation, native installation, touch/drag behaviour, signing and device testing remain unverified. These are updated source builds, not finished executable downloads. Test both installed apps side by side before distribution.
