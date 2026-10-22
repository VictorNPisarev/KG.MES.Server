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
SetupIconFile=kg.mes.sales.desktop\logo-kr2.ico

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
; Галочка "Создать ярлык на рабочем столе"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Берем ВСЕ файлы из папки, куда мы публиковали обёртку и сервер
; Важно: путь Source должен указывать на вашу папку publish\sales-desktop
Source: "publish\sales-desktop\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
; ✅ Добавляем иконку в установку
Source: "kg.mes.sales.desktop\logo-kr2.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Ярлык в меню Пуск (с иконкой)
Name: "{group}\Красная горка ОП"; Filename: "{app}\KG.MES.Sales.Desktop.exe"; IconFilename: "{app}\logo-kr2.ico"

; Ярлык на рабочем столе (с иконкой)
Name: "{autodesktop}\Красная горка ОП"; Filename: "{app}\KG.MES.Sales.Desktop.exe"; IconFilename: "{app}\logo-kr2.ico"; Tasks: desktopicon

[Run]
; Предложить запустить программу сразу после установки
Filename: "{app}\KG.MES.Sales.Desktop.exe"; Description: "{cm:LaunchProgram,Красная горка ОП}"; Flags: nowait postinstall skipifsilent