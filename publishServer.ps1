param
(
	[ValidateSet("Development", "Production")]
	[string]$Environment = "Development"
)

$projectPath = "kg.mes.server"
$publishPath = "publish\server"
$iisPath = if ($Environment -eq "Production")
{
	"\\server\inetpub\wwwroot\Kg.Mes.Server" 
}
else
{
	"C:\DEVelop\KG.MES.DeployTest\api" 
}

Write-Host "🚀 Publishing server ($Environment)..." -ForegroundColor Cyan

# Очистка
#Remove-Item -Recurse -Force $publishPath -ErrorAction SilentlyContinue

# Публикация
dotnet publish $projectPath -c Release -o $publishPath

if ($LASTEXITCODE -ne 0)
{
	Write-Host "Build failed with code $LASTEXITCODE" -ForegroundColor Red
	exit $LASTEXITCODE
}

Write-Host "Create webConfig..." -ForegroundColor Yellow

# Web.config
$webConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
	<system.webServer>
	  <handlers>
		<add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
	  </handlers>
	  <aspNetCore processPath="dotnet" arguments=".\kg.mes.server.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
		<environmentVariables>
		  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="$Environment" />
		</environmentVariables>
	  </aspNetCore>
	</system.webServer>
  </location>
</configuration>
"@

$webConfig | Out-File -FilePath "$publishPath\Web.config" -Encoding utf8

# Копирование на сервер (если Production)
## Активирую app_offline.htm (убираем #)
if (Test-Path "$iisPath\#app_offline.htm")
{
	Rename-Item -Path "$iisPath\#app_offline.htm" -NewName "app_offline.htm"
	Write-Host "App offline mode activated" -ForegroundColor Yellow
	Start-Sleep -Seconds 2
}

## Копирование
Write-Host "Copying to $iisPath..." -ForegroundColor Yellow
& robocopy $publishPath $iisPath /MIR /XF "app_offline.htm" /NP /NDL /NJH /NJS

## Деактивирую app_offline.htm (возвращаем #)
if (Test-Path "$iisPath\app_offline.htm")
{
	Rename-Item -Path "$iisPath\app_offline.htm" -NewName "#app_offline.htm"
	Write-Host "App online" -ForegroundColor Green
}

Write-Host "Published to $iisPath" -ForegroundColor Green