# TicketApi

REST API продажи билетов (ASP.NET Core, Clean Architecture: Domain / Application / Infrastructure / API).

## Что нужно запустить

1. **PostgreSQL** — создайте пустую БД `ticketapi`, пароль укажите в `API/appsettings.json` (`ConnectionStrings:DefaultConnection`).
2. **RabbitMQ** — локальный сервер, порт `5672`, `guest` / `guest`.
3. **SMTP** — заполните секцию `Email` в `API/appsettings.json` (для Gmail нужен пароль приложения).

## Миграции

Из корня решения:

```powershell
dotnet ef migrations add Init -p .\Infrastructure -s .\API -o Persistence/Migrations
dotnet ef database update -p .\Infrastructure -s .\API
```

Приложение миграции само не применяет.

## Запуск

```powershell
dotnet build
dotnet run --project API
```

Swagger: http://localhost:5080/swagger (кнопка **Authorize** — вставить JWT).

В Development при старте создаются роли и администратор: `admin@mail.com` / `Admin123`.

## Настройки (`API/appsettings.json`)

`ConnectionStrings`, `Jwt`, `Email`, `RabbitMq`, `Booking` (`HoldMinutes` = 10, `ExpiryCheckSeconds` = 30).

## Кеш

Выбран `IMemoryCache`: приложение работает в одном процессе, поэтому отдельный Redis-сервер не нужен. Кешируются `GET /api/events` и `GET /api/events/{id}` (DTO), абсолютный срок жизни 2 минуты; после создания/изменения события, брони, отмены и истечения кеш сбрасывается.

## Что выполнено

Части A–H: аутентификация и роли, категории, события, бронирование, RabbitMQ + письма по SMTP, фоновый сервис истечения броней, кеширование, PostgreSQL.
