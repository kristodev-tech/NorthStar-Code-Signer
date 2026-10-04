#define AppName "NorthStar Code Signer"
#define AppVersion "2.0.1"
[Setup]
AppId={{0A6F93DD-AAC7-4E02-BDDC-51712539D6E8}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=NorthStar Software Works
DefaultDirName={localappdata}\Programs\NorthStar Code Signer
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=NorthStarCodeSigner_Setup_v{#AppVersion}
SetupIconFile=..\assets\northstar.ico
UninstallDisplayIcon={app}\NorthStarCodeSigner.exe
LicenseFile=..\LICENSE
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
MinVersion=10.0
CloseApplications=yes
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
[Files]
Source: "..\NorthStarCodeSigner.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\NorthStarCodeSigner.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\USER_GUIDE.md"; DestDir: "{app}\docs"; Flags: ignoreversion
[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\NorthStarCodeSigner.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\NorthStarCodeSigner.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\NorthStarCodeSigner.exe"; Description: "Open {#AppName}"; Flags: nowait postinstall skipifsilent
