param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$serverProject = "kg.mes.sales"
$desktopProject = "kg.mes.sales.desktop"
$publishPath = "publish\sales-desktop"

Write-Host "1. Публикуем Blazor-сервер..." -ForegroundColor Cyan
dotnet publish $serverProject -c $Configuration -r win-x64 --self-contained true -o "$publishPath\server"

Write-Host "2. Публикуем обёртку..." -ForegroundColor Cyan
dotnet publish $desktopProject -c $Configuration -r win-x64 --self-contained true -o "$publishPath"

Write-Host "Готово: $publishPath" -ForegroundColor Green