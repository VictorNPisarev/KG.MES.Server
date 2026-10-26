param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$serverProject = "kg.mes.sales"
$desktopProject = "kg.mes.sales.desktop"
$publishPath = "publish\sales-desktop"

Write-Host "1. Публикую Blazor-сервер..." -ForegroundColor Cyan
dotnet publish $serverProject -c $Configuration -r win-x64 --self-contained true -o "$publishPath\server"

Write-Host "1. Копирую appsettings.Remote.json как appsettings.Production.json" -ForegroundColor Cyan
Copy-Item "kg.mes.sales\appsettings.Remote.json" "$publishPath\server\appsettings.Production.json" -Force

Write-Host "2. Публикую обёртку..." -ForegroundColor Cyan
dotnet publish $desktopProject -c $Configuration -r win-x64 --self-contained true -o "$publishPath"

Write-Host "Готово: $publishPath" -ForegroundColor Green