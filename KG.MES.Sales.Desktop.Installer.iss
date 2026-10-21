[Setup]
; Уникальный идентификатор (сгенерируйте в Inno Setup: Tools -> Generate GUID)
AppId={{5B8B263C-C1D0-4DDE-8633-95BFB9744B5F}}
AppName=Красная горка ОП
AppVersion=1.0
AppPublisher=Красная горка
DefaultDirName={autopf}\KG.MES Sales
DefaultGroupName=KG.MES Sales
; Куда сохранить готовый установщик
OutputDir=.\publish
OutputBaseFilename=КраснаяГоркаОП.Setup
; Сжатие
Compression=lzma2
SolidCompression=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
; Галочка "Создать ярлык на рабочем столе"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Берем ВСЕ файлы из папки, куда мы публиковали обёртку и сервер
; Важно: путь Source должен указывать на вашу папку publish\sales-desktop
Source: "publish\sales-desktop\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
; Ярлык в меню Пуск
Name: "{group}\Красная горка ОП"; Filename: "{app}\KG.MES.Sales.Desktop.exe"
; Ярлык на рабочем столе (создается, если выбрана галочка)
Name: "{autodesktop}\Красная горка ОП"; Filename: "{app}\KG.MES.Sales.Desktop.exe"; Tasks: desktopicon

[Run]
; Предложить запустить программу сразу после установки
Filename: "{app}\KG.MES.Sales.Desktop.exe"; Description: "{cm:LaunchProgram,Красная горка ОП}"; Flags: nowait postinstall skipifsilent