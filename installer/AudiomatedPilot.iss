#define MyAppName "Audiomated Pilot"
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif
#define MyAppPublisher "Harun Karagöz"
#define MyAppURL "https://github.com/aaron4311/audiomated-pilot"
; Name of the Task Scheduler task registered by installers before 1.0.2.
; Some machines silently refuse to create ONLOGON/RL LIMITED tasks (observed
; Access Denied even for a trivial exe, unrelated to this app specifically),
; so 1.0.2+ starts the watcher via a Startup-folder shortcut instead. Kept
; here only to clean up the old task on upgrade/uninstall.
#define LegacyTaskName "Audiomated Pilot Watcher"

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
; Starts the background watcher at logon. A Startup-folder shortcut needs no
; Task Scheduler API call, so it isn't subject to the ONLOGON/RL LIMITED
; restriction some machines silently enforce (see LegacyTaskName above).
Name: "{userstartup}\Audiomated Pilot Watcher"; Filename: "{app}\Audio.Service.exe"; WorkingDir: "{app}"

[Run]
; Clean up a pre-1.0.2 Task Scheduler registration, if one exists.
Filename: "schtasks.exe"; Parameters: "/End /TN ""{#LegacyTaskName}"""; Flags: runhidden; StatusMsg: "Cleaning up legacy scheduled task (if any)..."
Filename: "schtasks.exe"; Parameters: "/Delete /TN ""{#LegacyTaskName}"" /F"; Flags: runhidden
Filename: "{app}\Audio.GUI.exe"; Description: "Launch Audiomated Pilot"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Clean up a pre-1.0.2 Task Scheduler registration, if one exists.
Filename: "schtasks.exe"; Parameters: "/End /TN ""{#LegacyTaskName}"""; Flags: runhidden; RunOnceId: "StopLegacyWatcher"
Filename: "schtasks.exe"; Parameters: "/Delete /TN ""{#LegacyTaskName}"" /F"; Flags: runhidden; RunOnceId: "DeleteLegacyWatcherTask"
; Stop the running watcher so its exe file can be deleted.
Filename: "taskkill.exe"; Parameters: "/IM Audio.Service.exe /F"; Flags: runhidden; RunOnceId: "StopWatcher"

[Code]
// Files are copied during ssInstall, before any [Run] entry executes — a
// [Run]-based taskkill would run too late to unlock an already-running
// Audio.Service.exe for overwriting. InitializeSetup fires before Setup
// touches any files, so stop the watcher here instead.
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/IM Audio.Service.exe /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;
