[Setup]
AppName=flowOSD
AppVersion=3.0.0
AppVerName=flowOSD 3.0.0-dev510
AppCopyright=© 2021-2023, Albert Akhmetov
WizardStyle=modern
DefaultDirName={autopf}\flowOSD
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

VersionInfoVersion=3.0.0
VersionInfoProductName=flowOSD

DisableProgramGroupPage=yes
OutputBaseFilename=flowOSD-3.0.0-dev510

[Files]
Source: "..\output\publish\*.*"; DestDir: "{app}"; Flags: recursesubdirs
Source: "..\redist\WindowsAppRuntimeInstall.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autostartmenu}\flowOSD"; Filename: "{app}\flowOSD.exe"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "flowOSD"; ValueData: "{app}\flowOSD.exe"; Flags: uninsdeletevalue

[Run]
Filename: "{tmp}\WindowsAppRuntimeInstall.exe"; Parameters: "-q -f"; WorkingDir: "{tmp}"; StatusMsg: "Installing Windows App SDK runtime packages..."
Filename: "{app}\flowOSD.exe"; Description: "Run flowOSD"; Flags: postinstall shellexec
