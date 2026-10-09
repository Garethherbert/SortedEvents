# Native build and release checklist

Native output cannot be verified on this Linux environment: the .NET SDK/MAUI workloads, Windows SDK and Xcode are unavailable. Source was inspected for isolated identities, platform startup references, project references and manifest syntax; this is not compilation.

## Windows
- Install .NET 10 and Visual Studio MAUI/Windows tooling.
- Run Build-WindowsInstaller.ps1 with Inno Setup 6 installed.
- Check runtime startup, event persistence, date/time entry, export/import, file picker and local-network transfer.
- Sign the EXE/MSIX for distribution. A Microsoft Store reservation must provide a BigDaySorted identity and publisher before Store submission. The included manifest is a placeholder direct-build identity, not a reserved Store listing.

## Android
- Install .NET 10 MAUI Android, required SDK and JDK.
- Build APK first, test on a physical device; then sign an AAB using a BigDaySorted upload key.
- Check startup icon, keyboard/date/time behaviour, app data after restart, file pickers, invitation browsing, QR generation and scanning through the registered transfer URI.
- Play Console listing and package identity must be reserved separately from KeepMyTravels.

## Apple
- Use a Mac with compatible Xcode and .NET MAUI workloads.
- Reserve com.keepmytravels.bigdaysorted; set signing identity/provisioning profiles.
- Build an iOS simulator first; test real iPhone/iPad before publishing IPA.
- Build/sign Mac Catalyst APP/PKG; notarize direct-distribution releases as required.
- Check sandbox local-network permission, imports, URI activation and galleries opened in the browser.

## Cross-platform gaps
- Native and browser event stores are separate. Export/import is supported; automatic native cloud sync is not.
- Gift, RSVP and gallery screens currently open in the browser.
- URI cold-start handling and MAUI DatePicker/TimePicker version-specific behaviour need device verification.
- Existing KeepMyTravels transfer implementation is adapted into BigDayTransfer with its own URI scheme and MIME type. No KeepMyTravels production endpoints or credentials are present.

Reference: https://learn.microsoft.com/en-us/dotnet/maui/deployment and https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui?view=net-maui-10.0
