; NexAlbum kurulum programı (Inno Setup 6). Derleme: tools/yayin/paketle.ps1 -Kurulum
; Kullanıcı başına kurulur (yönetici izni istemez); .NET çalışma zamanı NexAlbum.exe'nin içindedir.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef AppNumericVersion
  #define AppNumericVersion AppVersion
#endif
#ifndef SourceDir
  #define SourceDir "..\out\NexAlbum"
#endif
#ifndef OutputDir
  #define OutputDir "..\out"
#endif
#ifndef Arch
  #define Arch "x64"
#endif

[Setup]
AppId={{6F0B7C2E-3B4A-4E8D-9A51-2C7D1E5F9A30}
AppName=NexAlbum
AppVersion={#AppVersion}
AppVerName=NexAlbum {#AppVersion}
VersionInfoVersion={#AppNumericVersion}
AppPublisher=Hasan Güler ve NexAlbum katkıcıları
AppPublisherURL=https://github.com/Eirahoutmoss/NexAlbum
AppSupportURL=https://github.com/Eirahoutmoss/NexAlbum/issues
AppUpdatesURL=https://github.com/Eirahoutmoss/NexAlbum/releases
AppCopyright=© 2026 Hasan Güler ve NexAlbum katkıcıları — GPL-3.0-or-later
DefaultDirName={autopf}\NexAlbum
DefaultGroupName=NexAlbum
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
#if Arch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
MinVersion=10.0.17763
LicenseFile={#SourceDir}\LICENSE.txt
SetupIconFile=..\src\NexAlbum.UI\Assets\NexAlbum.ico
UninstallDisplayIcon={app}\NexAlbum.exe
UninstallDisplayName=NexAlbum
OutputDir={#OutputDir}
OutputBaseFilename=NexAlbum-{#AppVersion}-kurulum
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ChangesAssociations=yes
CloseApplications=yes

[Languages]
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "association"; Description: ".albumproj dosyalarını NexAlbum ile aç"; GroupDescription: "Dosya ilişkilendirme:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\NexAlbum"; Filename: "{app}\NexAlbum.exe"
Name: "{autodesktop}\NexAlbum"; Filename: "{app}\NexAlbum.exe"; Tasks: desktopicon

[Registry]
Root: HKA; Subkey: "Software\Classes\.albumproj"; ValueType: string; ValueName: ""; ValueData: "NexAlbum.Proje"; Flags: uninsdeletevalue; Tasks: association
Root: HKA; Subkey: "Software\Classes\.albumproj\OpenWithProgids"; ValueType: string; ValueName: "NexAlbum.Proje"; ValueData: ""; Flags: uninsdeletevalue; Tasks: association
Root: HKA; Subkey: "Software\Classes\NexAlbum.Proje"; ValueType: string; ValueName: ""; ValueData: "NexAlbum albüm projesi"; Flags: uninsdeletekey; Tasks: association
Root: HKA; Subkey: "Software\Classes\NexAlbum.Proje\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\NexAlbum.exe,0"; Tasks: association
Root: HKA; Subkey: "Software\Classes\NexAlbum.Proje\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\NexAlbum.exe"" ""%1"""; Tasks: association

[Run]
Filename: "{app}\NexAlbum.exe"; Description: "{cm:LaunchProgram,NexAlbum}"; Flags: nowait postinstall skipifsilent
