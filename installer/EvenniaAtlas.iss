; Evennia Atlas - Inno Setup Installer Script
; Version 1.0.0
; Target: Windows x64, Per-User Installation

#define MyAppName "Evennia Atlas"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Wicked Media Solutions"
#define MyAppDescription "Visual World & Map Editor for Evennia"
#define MyAppExeName "EvenniaAtlas.exe"
#define MySourceDir "..\publish\temp"

[Setup]
AppId={{B8A7D3E2-F1C0-4B5A-9D8E-7F6A5B4C3D2E}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
VersionInfoVersion={#MyAppVersion}
VersionInfoProductVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
OutputDir=.\
OutputBaseFilename=EvenniaAtlas-Setup-1.0.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\EvenniaAtlas\EvenniaAtlas.ico
WizardSmallImageFile=WizardSmallImage.bmp
WizardImageFile=WizardImageFile.bmp
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
ShowLanguageDialog=no
DisableWelcomePage=no
DisableProgramGroupPage=yes
AllowNoIcons=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Messages]
WelcomeLabel1=Welcome to the Evennia Atlas Setup Wizard
WelcomeLabel2=This will install Evennia Atlas on your computer.