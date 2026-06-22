# KG.MES.Server

Серверная часть производственной системы (MES). API для управления заказами, участками, снабжением.

## Технологии

- .NET 10
- ASP.NET Core WebAPI
- Entity Framework Core
- PostgreSQL
- SignalR (WebSocket)

## Требования

- .NET 10 SDK
- PostgreSQL 14+

## Установка

```bash
git clone https://github.com/ваш-репозиторий/KG.MES.Server.git
cd KG.MES.Server
dotnet restore
```

## Настройка

Установите переменные окружения:
```bash
set DB_HOST=localhost
set DB_PORT=5432
set DB_NAME=KG_MES
set DB_USER=postgres
set DB_PASSWORD=your_password
```

Примените миграции:

```bash
dotnet ef database update --project KG.MES.Server --startup-project KG.MES.Server
```

## Запуск

```bash
dotnet run --project KG.MES.Server
```

## Release
```bash
dotnet publish -c Release -o \\server\inetpub\wwwroot\Kg.Mes.Server
```

## Development
```bash
dotnet publish -o C:\DEVelop\KG.MES.DeployTest\api
```

## Endpoints

Swagger UI: http://localhost:5000/swagger

SignalR Hub: http://localhost:5000/hub/notification

## Структура проекта

KG.MES.Server/          # WebAPI
KG.MES.Shared/          # Модели и DTO

## Лицензия

MIT