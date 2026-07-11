#ifndef MyAppVersion
  #error MyAppVersion is required
#endif
#ifndef MyBuildNumber
  #error MyBuildNumber is required
#endif
#ifndef SourceExe
  #error SourceExe is required
#endif
#ifndef ProjectRoot
  #error ProjectRoot is required
#endif
#ifndef OutputDir
  #error OutputDir is required
#endif

[Setup]
AppId={{B855A383-8B1F-46A6-A39E-4C7D529C57C1}
AppName=Minecraft Server Launcher
AppVersion={#MyAppVersion}
AppVerName=Minecraft Server Launcher v{#MyAppVersion} (build {#MyBuildNumber})
AppPublisher=Mangom72
AppPublisherURL=https://github.com/Mangom72/mc-server-launcher
AppSupportURL=https://github.com/Mangom72/mc-server-launcher/issues
AppUpdatesURL=https://github.com/Mangom72/mc-server-launcher/releases/latest
DefaultDirName={autopf}\Minecraft Server Launcher
DefaultGroupName=Minecraft Server Launcher
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename=Minecraft-Server-Launcher-Setup-v{#MyAppVersion}
SetupIconFile={#ProjectRoot}\launcher-icon.ico
UninstallDisplayIcon={app}\Minecraft-Server-Launcher.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
UsePreviousAppDir=yes
Uninstallable=yes
VersionInfoVersion={#MyBuildNumber}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoDescription=Minecraft Server Launcher installer
VersionInfoProductName=Minecraft Server Launcher
LicenseFile={#ProjectRoot}\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceExe}"; DestDir: "{app}"; DestName: "Minecraft-Server-Launcher.exe"; Flags: ignoreversion
Source: "{#ProjectRoot}\obj\installed.mode"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ProjectRoot}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ProjectRoot}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ProjectRoot}\PRIVACY.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Minecraft Server Launcher"; Filename: "{app}\Minecraft-Server-Launcher.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Minecraft Server Launcher"; Filename: "{app}\Minecraft-Server-Launcher.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Minecraft-Server-Launcher.exe"; Description: "{cm:LaunchProgram,Minecraft Server Launcher}"; Flags: nowait postinstall skipifsilent
