; Inno Setup — managed SSH tunnel; no external ssh.exe or open PostgreSQL port required
; Build: powershell -File installer\build-installer.ps1

#define MyAppName "الأمل.AB ERP"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Alamal AB"
#define MyAppExeName "ERPSystem.exe"
#define PublishDir "..\publish\installer-input"
#define DbHost "65.21.136.217"
#define AppIcon "..\Assets\Brand\app-icon.ico"

[Setup]
AppId={{A3F9C2E1-8B4D-4F6A-9C1E-2D5E7A8B9C0D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\AlamalAB\ERPSystem
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\publish
OutputBaseFilename=AlamalAB-ERP-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
SetupIconFile={#AppIcon}
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات:"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#AppIcon}"; DestDir: "{app}\Assets\Brand"; Flags: ignoreversion
Source: "secrets\erp_tunnel_key"; DestDir: "{app}\Config"; DestName: "erp_tunnel_key"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\Brand\app-icon.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\Brand\app-icon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "تشغيل الأمل.AB ERP"; Flags: nowait postinstall skipifsilent

[Code]
var
  DbPasswordPage: TInputQueryWizardPage;

function JsonEscape(const Value: string): string;
var
  I: Integer;
  C: string;
begin
  Result := '';
  for I := 1 to Length(Value) do
  begin
    C := Value[I];
    if C = '\' then
      Result := Result + '\\'
    else if C = '"' then
      Result := Result + '\"'
    else
      Result := Result + C;
  end;
end;

procedure InitializeWizard;
begin
  DbPasswordPage := CreateInputQueryPage(wpSelectDir,
    'إعداد الاتصال بقاعدة البيانات',
    'اتصال آمن بالسحابة عبر نفق مُدار داخل التطبيق',
    'أدخل كلمة مرور مستخدم قاعدة البيانات erp_app.' + #13#10 +
    'لا يحتاج التطبيق إلى فتح منفذ PostgreSQL أو تثبيت OpenSSH.');
  DbPasswordPage.Add('كلمة مرور erp_app:', True);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = DbPasswordPage.ID then
  begin
    if Trim(DbPasswordPage.Values[0]) = '' then
    begin
      MsgBox('كلمة مرور قاعدة البيانات مطلوبة للمتابعة.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Password, Json: string;
begin
  if CurStep = ssPostInstall then
  begin
    Password := JsonEscape(Trim(DbPasswordPage.Values[0]));
    Json :=
      '{' + #13#10 +
      '  "ConnectionStrings": {' + #13#10 +
      '    "DefaultConnection": "Host=localhost;Port=5433;Database=erp_pro;Username=erp_app;Password=' + Password + ';SSL Mode=Disable"' + #13#10 +
      '  },' + #13#10 +
      '  "SshTunnel": {' + #13#10 +
      '    "Enabled": true,' + #13#10 +
      '    "SshHost": "65.21.136.217",' + #13#10 +
      '    "SshPort": 2727,' + #13#10 +
      '    "SshUser": "ubuntu",' + #13#10 +
      '    "LocalPort": 5433,' + #13#10 +
      '    "RemoteHost": "localhost",' + #13#10 +
      '    "RemotePort": 5432,' + #13#10 +
      '    "IdentityFile": "Config/erp_tunnel_key"' + #13#10 +
      '  },' + #13#10 +
      '  "Desktop": { "ConnectionMode": "ManagedSshTunnel" }' + #13#10 +
      '}';
    SaveStringToFile(ExpandConstant('{app}\appsettings.Local.json'), Json, False);
  end;
end;
