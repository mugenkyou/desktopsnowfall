; Windows installer for desktopsnowfall (Inno Setup 6.3+, a free build-time tool; nothing extra is installed).
; Per-user install: no administrator prompt. Includes an uninstaller (Settings → Apps).
;
; Build (after `dotnet publish`, see .github/workflows/release.yml):
;   iscc /DAppVersion=1.2.0 /DSourceExe=out\win-x64\desktopsnowfall.exe installer\desktopsnowfall.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceExe
  #define SourceExe "..\out\win-x64\desktopsnowfall.exe"
#endif

[Setup]
AppId={{8B1F3C2E-6C2A-4F5B-9B47-3D2F9E1A7C55}
AppName=desktopsnowfall
AppVersion={#AppVersion}
AppVerName=desktopsnowfall {#AppVersion}
AppPublisher=mugenkyou
AppPublisherURL=https://github.com/mugenkyou/desktopsnowfall
AppSupportURL=https://github.com/mugenkyou/desktopsnowfall/issues
DefaultDirName={localappdata}\Programs\desktopsnowfall
DisableProgramGroupPage=yes
DisableDirPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\out
OutputBaseFilename=desktopsnowfall-setup-x64-v{#AppVersion}
SetupIconFile=..\windows\app.ico
UninstallDisplayIcon={app}\desktopsnowfall.exe
UninstallDisplayName=desktopsnowfall
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=force
LicenseFile=..\LICENSE

[Tasks]
Name: "startup"; Description: "Start desktopsnowfall when Windows starts"; Flags: unchecked

[Files]
Source: "{#SourceExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\desktopsnowfall"; Filename: "{app}\desktopsnowfall.exe"

[Registry]
; Optional autostart (the same value the app's "Start with Windows" menu item uses)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "desktopsnowfall"; \
  ValueData: """{app}\desktopsnowfall.exe"""; Tasks: startup
; On uninstall: remove the autostart entry and the app's settings, whether or not they were created here
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "desktopsnowfall"; Flags: dontcreatekey uninsdeletevalue
Root: HKCU; Subkey: "Software\desktopsnowfall"; Flags: dontcreatekey uninsdeletekey

[Run]
Filename: "{app}\desktopsnowfall.exe"; Description: "Let it snow now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Quit a running copy so its file can be removed
Filename: "{sys}\taskkill.exe"; Parameters: "/im desktopsnowfall.exe /f"; Flags: runhidden; RunOnceId: "StopSnow"
