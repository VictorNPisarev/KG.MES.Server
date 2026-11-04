param(
	[Parameter(Mandatory=$true)]
	[ValidateSet("main", "masters", "supply", "sales")]
	[string]$Project,
	
	[ValidateSet("debug", "release")]
	[string]$Configuration = "debug",
	
	[ValidateSet("Development", "Production")]
	[string]$Environment = "Development"
)

$projectPath = "kg.mes.$Project"
$publishPath = "publish\$Project"
$iisPath = if ($Environment -eq "Production") { "\\server\inetpub\wwwroot\Kg.Mes.Apps\$Project" } else { "C:\DEVelop\KG.MES.DeployTest\portal\$Project" }
$server = if ($Environment -eq "Production") { "192.168.0.254" } else { "localhost" }

Write-Host "Publishing $Project ($Configuration) for $Environment..." -ForegroundColor Cyan

# 1. Очистка
#Remove-Item -Recurse -Force $publishPath -ErrorAction SilentlyContinue

# 2. Публикация
dotnet publish $projectPath -c $Configuration -o $publishPath

if ($LASTEXITCODE -ne 0)
{
	Write-Host "Build failed with code $LASTEXITCODE" -ForegroundColor Red
	exit $LASTEXITCODE
}

Write-Host "Create webConfig..." -ForegroundColor Yellow

# 3. Замена Web.config с правильным PathBase
$webConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
	<location path="." inheritInChildApplications="false">
		<system.webServer>
			<handlers>
				<add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
			</handlers>
			<aspNetCore processPath="dotnet" arguments=".\KG.MES.$Project.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
				<environmentVariables>
					<environmentVariable name="ASPNETCORE_ENVIRONMENT" value="$Environment" />
					<environmentVariable name="ASPNETCORE_URLS" value="http://*:5000" />
				</environmentVariables>
			</aspNetCore>
		</system.webServer>
	</location>
</configuration>
"@

$webConfig | Out-File -FilePath "$publishPath\Web.config" -Encoding utf8

# 4. Копирование на сервер (если Production)
# 4.1. Активирую app_offline.htm (убираем #)
if (Test-Path "$iisPath\#app_offline.htm")
{
	Rename-Item -Path "$iisPath\#app_offline.htm" -NewName "app_offline.htm"
	Write-Host "App offline mode activated" -ForegroundColor Yellow
	Start-Sleep -Seconds 5
}

#4.2 Копирую файлы (удаляются все, которых нет в источнике, исключая app_offline.htm)
Write-Host "Copying to $server..." -ForegroundColor Yellow
& robocopy $publishPath $iisPath /MIR /XF "app_offline.htm" "license.key" /NP /NDL /NJH /NJS

# 3. Деактивирую app_offline.htm (возвращаем #)
if (Test-Path "$iisPath\app_offline.htm")
{
	Rename-Item -Path "$iisPath\app_offline.htm" -NewName "#app_offline.htm"
	Write-Host "App online" -ForegroundColor Green
}

Write-Host "Published to $iisPath" -ForegroundColor Green
