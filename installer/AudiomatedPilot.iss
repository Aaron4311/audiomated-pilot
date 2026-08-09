#define MyAppName "Audiomated Pilot"
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif
#define MyAppPublisher "Harun Karagöz"
#define MyAppURL "https://github.com/aaron4311/audiomated-pilot"
#define TaskName "Audiomated Pilot Watcher"

[Setup]
AppId={{9F2C6E3B-9B7B-4B6B-9B0A-6C5F7A2B4E10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\Audiomated Pilot
DefaultGroupName=Audiomated Pilot
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=AudiomatedPilotSetup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\assets\icon.ico
UninstallDisplayIcon={app}\Audio.GUI.exe
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "publish\cli\Audio.CLI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\gui\Audio.GUI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\service\Audio.Service.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Audiomated Pilot"; Filename: "{app}\Audio.GUI.exe"
Name: "{group}\Uninstall Audiomated Pilot"; Filename: "{uninstallexe}"

[Run]
; Stop any already-running watcher before (re)installing, ignore failure if none exists.
Filename: "schtasks.exe"; Parameters: "/End /TN ""{#TaskName}"""; Flags: runhidden; StatusMsg: "Stopping existing watcher (if any)..."
Filename: "schtasks.exe"; Parameters: "/Create /TN ""{#TaskName}"" /TR ""\""{app}\Audio.Service.exe\"""" /SC ONLOGON /RL LIMITED /F"; Flags: runhidden; StatusMsg: "Registering background watcher..."
Filename: "{app}\Audio.GUI.exe"; Description: "Launch Audiomated Pilot"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "schtasks.exe"; Parameters: "/End /TN ""{#TaskName}"""; Flags: runhidden; RunOnceId: "StopWatcher"
Filename: "schtasks.exe"; Parameters: "/Delete /TN ""{#TaskName}"" /F"; Flags: runhidden; RunOnceId: "DeleteWatcherTask"
