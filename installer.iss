; FramePulse - Inno Setup
#define MyAppName "FramePulse"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "R3G1S"
#define MyAppExeName "FpsOverlay.exe"
#define MyAppAssocName "FramePulse Overlay"
#define MyAppAssocExt ".fpulse"
#define CfgDir "{userappdata}\FpsOverlay"

[Setup]
AppId={{8F3C2A91-4B7E-4D65-9C1A-2E7F0B5D8A34}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/R3G1S
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=..\installer
OutputBaseFilename=FramePulse-Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=dist\icon.ico
DisableProgramGroupPage=no
LicenseFile=
ShowLanguageDialog=auto
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Запускать вместе с Windows"; GroupDescription: "Автозапуск:"
Name: "config"; Description: "Применить настройки по умолчанию (текущие параметры оверлея)"; GroupDescription: "Конфигурация:"; Flags: unchecked

[Files]
Source: "dist\FpsOverlay.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "dist\PresentMon.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "dist\icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "config.default.ini"; DestDir: "{tmp}"; Flags: dontcopy

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "Игровой оверлей FPS / ЦП / ГП"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup; Comment: "FramePulse"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "FramePulse"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/F /IM {#MyAppExeName}"; RunOnceId: "KillOverlay"; Flags: runhidden
Filename: "taskkill"; Parameters: "/F /IM PresentMon.exe"; RunOnceId: "KillPM"; Flags: runhidden

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec('taskkill', '/F /IM ' + '{#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill', '/F /IM PresentMon.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  CfgPath, CfgDir: String;
begin
  if CurStep = ssPostInstall then
  begin
    if WizardIsTaskSelected('config') then
    begin
      CfgDir := ExpandConstant('{#CfgDir}');
      CfgPath := CfgDir + '\config.ini';
      if not DirExists(CfgDir) then
        ForceDirectories(CfgDir);
      ExtractTemporaryFile('config.default.ini');
      CopyFile(ExpandConstant('{tmp}\config.default.ini'), CfgPath, False);
    end
    else
    begin
      CfgDir := ExpandConstant('{#CfgDir}');
      CfgPath := CfgDir + '\config.ini';
      if not DirExists(CfgDir) then
        ForceDirectories(CfgDir);
      if not FileExists(CfgPath) then
      begin
        ExtractTemporaryFile('config.default.ini');
        CopyFile(ExpandConstant('{tmp}\config.default.ini'), CfgPath, False);
      end;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // config.ini оставляем у пользователя; при желании удалить вручную
  end;
end;
