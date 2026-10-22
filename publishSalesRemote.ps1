param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$projectPath = "kg.mes.sales"
$publishPath = "publish\sales-remote"

Write-Host "Publishing Sales for REMOTE managers..." -ForegroundColor Cyan

dotnet publish $projectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -o $publishPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed" -ForegroundColor Red
    exit 1
}

# Копируем appsettings.Remote.json как appsettings.Production.json
# чтобы при ASPNETCORE_ENVIRONMENT=Production использовался внешний API
Copy-Item "kg.mes.sales\appsettings.Remote.json" "$publishPath\appsettings.Production.json" -Force

# Bat-файл для запуска
@"
@echo off
set ASPNETCORE_ENVIRONMENT=Production
start "" "KG.MES.Sales.exe"
"@ | Out-File -FilePath "$publishPath\start.bat" -Encoding ascii

Write-Host "Output: $publishPath" -ForegroundColor Green