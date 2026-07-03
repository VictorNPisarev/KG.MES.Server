## Восстановление
```bash
## 1. Запись пароля в переменные среды
$env:PGPASSWORD = "*********"
## 2. Перейти в папку с бэкапом (либо сразу шаг 3 с полным адресом бэкапа)
cd D:\KG.MES.AutoBackups
## 3. Восстановление с очисткой существующей БД
& "C:\Program Files\PostgreSQL\18\bin\pg_restore.exe" -U postgres -h localhost -d KgMes --clean --if-exists KgMes_20260701_092058.dump
```