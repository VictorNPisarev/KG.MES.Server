# backup_script.ps1
# Скрипт автоматического бэкапа PostgreSQL (упрощённая версия)

# Параметры подключения
$pgHost = "192.168.0.254"
$pgPort = "5432"
$pgUser = "postgres"
$pgPassword = "WGbbYT8t!q"
$database = "KgMes"

# Путь к pg_dump
$pgDumpPath = "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe"

# Папка для бэкапов
$backupDir = "D:\KG.MES.AutoBackups"

# Дата для имени файла
$date = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = "$backupDir\KgMes_$date.dump"

# Количество дней хранения
$retentionDays = 30

# ============================================
# Начало скрипта
# ============================================

Write-Host "=== Начало бэкапа базы $database ==="

# Проверка существования pg_dump
if (!(Test-Path $pgDumpPath)) {
    Write-Host "ОШИБКА: pg_dump не найден по пути $pgDumpPath"
    Write-Host "Проверьте версию PostgreSQL"
    exit 1
}

# Проверка папки для бэкапов
if (!(Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir -Force
    Write-Host "Создана папка: $backupDir"
}

# Установка пароля для pg_dump
$env:PGPASSWORD = $pgPassword

# Выполнение бэкапа
Write-Host "Создание бэкапа: $backupFile"

& $pgDumpPath -h $pgHost -p $pgPort -U $pgUser -d $database -F c -b -v -f $backupFile

if ($LASTEXITCODE -eq 0) {
    $fileSize = (Get-Item $backupFile).Length / 1MB
    Write-Host "Бэкап создан успешно! Размер: $([math]::Round($fileSize, 2)) MB"
} else {
    Write-Host "ОШИБКА при создании бэкапа. Код ошибки: $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Удаление старых бэкапов
Write-Host "Удаление бэкапов старше $retentionDays дней"

$oldBackups = Get-ChildItem -Path $backupDir -Filter "KgMes_*.dump" | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$retentionDays) }

if ($oldBackups.Count -gt 0) {
    foreach ($file in $oldBackups) {
        Remove-Item -Path $file.FullName -Force
        Write-Host "Удалён: $($file.Name)"
    }
    Write-Host "Удалено $($oldBackups.Count) старых бэкапов"
} else {
    Write-Host "Старых бэкапов для удаления нет"
}

Write-Host "=== Бэкап завершён ==="