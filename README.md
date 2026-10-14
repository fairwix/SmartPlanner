# SmartPlanner API

REST API на ASP.NET Core для управления пользователями, целями и челленджами. В проекте также есть достижения, загрузка файлов, SignalR-уведомления и дополнительные API для продуктов, постов и сообщений.

## Стек и структура

- .NET 9, ASP.NET Core Web API
- PostgreSQL и Entity Framework Core
- MediatR, FluentValidation, AutoMapper
- JWT bearer-аутентификация и SignalR

```text
SmartPlanner.API/             HTTP API, контроллеры, SignalR и middleware
SmartPlanner.Application/     команды, запросы, обработчики и DTO
SmartPlanner.Domain/          доменные сущности и правила
SmartPlanner.Infrastructure/  EF Core, PostgreSQL и миграции
SmartPlanner.Tests/           модульные и интеграционные тесты
```

## Запуск локально

Нужны .NET 9 SDK и работающий PostgreSQL. Репозиторий не содержит Docker Compose.

1. Клонируйте репозиторий и перейдите в его каталог:

   ```bash
   git clone https://github.com/fairwix/SmartPlanner.git
   cd SmartPlanner
   ```

2. Создайте базу данных `smartplanner` в PostgreSQL. Настройте строку подключения и секрет JWT через User Secrets, чтобы не хранить их в репозитории:

   ```bash
   dotnet user-secrets set "ConnectionStrings:PostgreSQL" "Host=localhost;Port=5432;Database=smartplanner;Username=<user>;Password=<password>" --project SmartPlanner.API
   dotnet user-secrets set "JwtSettings:Secret" "<случайный приватный ключ длиной не менее 32 байт>" --project SmartPlanner.API
   ```

   Приложение завершает запуск, если `JwtSettings:Secret` отсутствует, слишком короткий или совпадает с известным примером из репозитория. Для production задайте свой случайный секрет через secret store или переменную `JwtSettings__Secret`.

3. Запустите API:

   ```bash
   dotnet run --project SmartPlanner.API
   ```

   При запуске приложение применяет EF Core-миграции. HTTP-адрес из конфигурации — `http://localhost:5047`. В окружении `Development` Swagger UI доступен по адресу `http://localhost:5047/swagger`.

В `SmartPlanner.API/appsettings.Development.json` указана локальная строка подключения с примерными реквизитами `postgres/postgres`. Переопределите её User Secrets или переменной окружения перед запуском. Для отправки email в Development включён файловый режим: письма сохраняются в каталоге `Emails`.

### Создание миграции

Миграции находятся в `SmartPlanner.Infrastructure/Data/Migrations`. Создать новую миграцию можно командой:

```bash
dotnet ef migrations add <MigrationName> \
  --project SmartPlanner.Infrastructure \
  --startup-project SmartPlanner.API \
  --output-dir Data/Migrations
```

## Конфигурация

Ключевые параметры задаются стандартной конфигурацией ASP.NET Core: `appsettings*.json`, User Secrets или переменными окружения.

| Параметр | Назначение |
|---|---|
| `ConnectionStrings:PostgreSQL` | Подключение к базе PostgreSQL |
| `JwtSettings:Secret` | Приватный ключ подписи JWT, не менее 32 байт |
| `JwtSettings:Issuer` | Ожидаемый issuer токена |
| `JwtSettings:Audience` | Ожидаемая audience токена |
| `CorsSettings:AllowedOrigins` | Список разрешённых web-origin, например `CorsSettings__AllowedOrigins__0` |
| `CorsSettings:AllowCredentials` | Разрешать ли credentials для указанных origin |

CORS ограничен origin из `CorsSettings:AllowedOrigins`; wildcard origin не используется. В production настройте список адресов своего frontend и отдельный JWT-секрет.

## Аутентификация

Защищённые запросы принимают JWT в заголовке:

```http
Authorization: Bearer <access-token>
```

Основные маршруты аутентификации находятся под `/api/auth`:

| Метод | Путь | Назначение |
|---|---|---|
| `POST` | `/register` | Регистрация |
| `POST` | `/login` | Вход |
| `POST` | `/refresh` | Обновление пары токенов |
| `POST` | `/logout` | Выход, требует авторизации |
| `POST` | `/revoke` | Отзыв refresh-токена, требует авторизации |
| `POST` | `/forgot-password` | Запрос сброса пароля |
| `POST` | `/reset-password` | Установка нового пароля |
| `POST` | `/change-password` | Смена пароля, требует авторизации |
| `GET` | `/confirm-email` | Подтверждение email по `userId` и `token` |
| `GET` | `/profile` | Профиль текущего пользователя |

Регистрация доступна без входа. `POST /api/users` предназначен только для администратора; при совпадении email или username API возвращает ошибку, не выдавая профиль существующего аккаунта.

## Основные API-маршруты

Если не указано иное, методы ниже требуют JWT.

### Цели — `/api/goals`

| Метод | Путь | Назначение |
|---|---|---|
| `GET` | `/` | Цели текущего пользователя; поддерживает `page`, `pageSize`, `category`, `priority`, `completed`, `search`, `sortBy`, `sortOrder` |
| `GET` | `/{id}` | Получение своей цели |
| `POST` | `/` | Создание цели |
| `PUT` | `/{id}` | Обновление своей цели |
| `DELETE` | `/{id}` | Удаление своей цели |
| `POST` | `/{id}/progress` | Добавление прогресса |
| `POST` | `/bulk` | Массовое создание целей для текущего пользователя |
| `PUT` | `/bulk` | Массовое обновление только своих целей |
| `DELETE` | `/bulk` | Массовое удаление только своих целей |
| `GET` | `/admin/all` | Маршрут-заглушка: сейчас возвращает пустой `200 OK` |

Для создания цели передаются `title`, `description`, `category`, `priority`, `dueDate` и `targetValue`. Владельца API определяет по JWT, а не по переданному клиентом `userId`.

### Челленджи — `/api/challenges`

| Метод | Путь | Назначение |
|---|---|---|
| `GET` | `/{id}` | Получение челленджа |
| `POST` | `/` | Создание челленджа |
| `POST` | `/{id}/join` | Участие в челлендже |
| `POST` | `/{id}/progress` | Добавление прогресса участника |
| `POST` | `/{id}/leave` | Выход из челленджа |

### Файлы — `/api/files`

Контроллер требует авторизацию. Обычная загрузка ограничена 50 000 000 байтами, `upload-stream` — примерно 2,1 млрд байт на весь multipart-запрос; одновременная большая загрузка ограничена двумя запросами.

| Метод | Путь | Назначение |
|---|---|---|
| `POST` | `/upload` | Загрузка одного файла, до 50 МБ |
| `POST` | `/upload-multiple` | Загрузка до 10 файлов, лимит запроса 100 МБ |
| `POST` | `/upload-stream` | Потоковая загрузка большого файла |
| `POST` | `/upload/chunked/start` | Создание chunked-сессии |
| `POST` | `/upload/chunked` | Передача чанка; лимит запроса 50 МБ |
| `GET` | `/upload/{uploadId}/progress` | Состояние chunked-загрузки текущего пользователя |
| `GET` | `/{id}` | Скачивание файла с проверкой доступа |
| `GET` | `/{id}/stream` | Потоковое скачивание файла с проверкой доступа |
| `GET` | `/{id}/thumbnail` | Миниатюра изображения |
| `GET` | `/{id}/info` | Метаданные файла |
| `DELETE` | `/{id}` | Удаление файла владельцем или администратором |
| `POST` | `/check-duplicate` | Проверка дубликата файла |

Для chunked-загрузки сначала отправьте JSON с `fileName`, `fileSize`, `totalChunks` и `fileHash` на `/upload/chunked/start`. Используйте возвращённый `uploadId` для POST multipart-запросов на `/upload/chunked` с полями `chunk`, `uploadId`, `chunkIndex`, `totalChunks` и `fileName`. Не создавайте `uploadId` самостоятельно: чанки принимаются только для серверной сессии текущего пользователя.

### Пользователи, достижения и аудит

- `/api/users`: получение, обновление и удаление профиля; создание пользователя и блокировка доступны администраторам согласно атрибутам авторизации контроллера.
- `/api/achievements`: список достижений доступен публично; пользовательские достижения видны владельцу или администратору; проверка достижений требует авторизации.
- `/api/admin/audit`: просмотр журналов аудита доступен только администраторам.
- `/api/admin/AdminAchievements/award/{userId}/{achievementId}`: выдача достижения администратором.

В проекте также есть авторизованные контроллеры `/api/products`, `/api/posts` и `/api/messages` для соответствующих сущностей и вложений.

## SignalR

Хабы доступны по адресам `/hubs/notifications` и `/hubs/file`. JWT передаётся клиентом SignalR стандартным способом для используемой платформы; в браузере для WebSocket/SSE обычно применяется query-параметр `access_token`.

NotificationHub автоматически добавляет соединение в личную группу пользователя. Дополнительная подписка разрешена только на собственную группу `user_<id>`; группа `admin_notifications` доступна только роли `Admin`. Клиент не может подписаться на уведомления другого пользователя.

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:7273/hubs/notifications", {
    accessTokenFactory: () => accessToken
  })
  .build();

await connection.start();
connection.on("ReceiveNotification", notification => console.log(notification));
```

## Тесты

Запуск всех unit- и integration-тестов:

```bash
dotnet test SmartPlanner.sln
```

API integration-тесты используют тестовый хост и in-memory базу данных; реальная PostgreSQL для этого набора тестов не нужна.

## Лицензия

См. файл [LICENSE](LICENSE).
