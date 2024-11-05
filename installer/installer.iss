#define AppName 'flowOSD'

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppCopyright=© 2021-2024, Albert Akhmetov
WizardStyle=modern
DefaultDirName={autopf}\{#AppName}
UninstallDisplayIcon={app}\flowOSD.exe
Compression=lzma2/max
SolidCompression=yes
OutputDir=..\output
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
LicenseFile=..\LICENSE

AppPublisher=Albert Akhmetov
AppPublisherURL=https://albertakhmetov.com

AppSupportURL=https://github.com/albertakhmetov/flowOSD

VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}

DisableProgramGroupPage=yes
OutputBaseFilename={#AppName}-{#AppVersion}

[Files]
Source: "..\output\publish\*.*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{autostartmenu}\flowOSD"; Filename: "{app}\flowOSD.exe"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "flowOSD"; ValueData: "{app}\flowOSD.exe"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\flowOSD.exe"; Description: "Run {#AppName}"; Flags: postinstall shellexec
