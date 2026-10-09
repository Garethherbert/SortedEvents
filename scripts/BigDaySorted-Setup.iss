#ifndef AppName
#define AppName "BigDaySorted"
#endif
#if AppName == "LiveDaysSorted"
#define ProductGuid "{{3B3F2150-5315-4A5A-8B3A-57291B03D846}"
#else
#define ProductGuid "{{9DEB415B-9C6A-4D06-9EF6-779A6D1F8741}"
#endif
#ifndef PublishDir
#define PublishDir "..\artifacts\" + AppName + "\windows\build-5"
#endif
#ifndef BuildNumber
#define BuildNumber "5"
#endif
[Setup]
AppId={#ProductGuid}
AppName={#AppName}
AppVersion=0.1.4.{#BuildNumber}
AppPublisher=KeepMyTravels LTD
DefaultDirName={localappdata}\Programs\{#AppName}
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
OutputDir=..\artifacts\installers
OutputBaseFilename={#AppName}-0.1.4-build{#BuildNumber}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppName}.App.exe
[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppName}.App.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppName}.App.exe"; Tasks: desktopicon
[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; Flags: unchecked
[Run]
Filename: "{app}\{#AppName}.App.exe"; Description: "Open {#AppName}"; Flags: nowait postinstall skipifsilent
