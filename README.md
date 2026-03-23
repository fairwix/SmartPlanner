# 🎯 SmartPlanner API

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-4169E1.svg)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Swagger](https://img.shields.io/badge/Swagger-3.0-85EA2D.svg)](https://swagger.io/)

> **SmartPlanner** — REST API для управления целями, челленджами и достижениями с поддержкой real-time уведомлений, файлов и продвинутой системы аутентификации.
---

## 📋 Оглавление

- [✨ Особенности](#-особенности)
- [🏗️ Архитектура](#️-архитектура)
- [🚀 Быстрый старт](#-быстрый-старт)
- [🔐 Аутентификация и авторизация](#-аутентификация-и-авторизация)
- [📡 API Endpoints](#-api-endpoints)
- [🔔 Real-time уведомления (SignalR)](#-real-time-уведомления-signalr)
- [📁 Работа с файлами](#-работа-с-файлами)
- [🧪 Тестирование](#-тестирование)
- [⚙️ Конфигурация](#️-конфигурация)
- [🛡️ Безопасность](#️-безопасность)
- [📊 Мониторинг и логирование](#-мониторинг-и-логирование)
- [🤝 Вклад в проект](#-вклад-в-проект)
- [📄 Лицензия](#-лицензия)

---

## ✨ Особенности

### 🔐 Безопасность и аутентификация
- **JWT Authentication** с поддержкой refresh-токенов
- **Подтверждение email** при регистрации
- **Восстановление пароля** через защищённые токены
- **Блокировка пользователей** и управление сессиями
- **Rate Limiting** для защиты от brute-force атак
- **Аудит безопасности** — логирование всех критических событий

### 🎯 Управление целями
- Полноценный CRUD для целей с валидацией
- Отслеживание прогресса с историей изменений
- Категории и приоритеты целей
- **Массовые операции** (bulk create/update/delete)
- Автоматическое начисление наград за завершение

### 🏆 Система достижений и челленджей
- Гибкая система ачивок с условиями выполнения
- Индивидуальные и групповые челленджи
- Статистика участия и прогресс в реальном времени
- Автоматическая проверка и награждение достижениями

### 📁 Продвинутая работа с файлами
- Загрузка файлов до **2 ГБ** с поддержкой chunked upload
- **Дедупликация** файлов по SHA256-хешу
- Автоматическая генерация **thumbnail** для изображений
- Удаление метаданных (EXIF) для публичных файлов
- Оптимизация изображений с настройкой качества
- Поддержка публичных и приватных файлов с контролем доступа

### 🔔 Real-time возможности
- **SignalR Hubs** для мгновенных уведомлений
- Отслеживание прогресса загрузки в реальном времени
- Уведомления о событиях системы
- Поддержка групповой рассылки сообщений

### 🧩 Дополнительные возможности
- **CQRS паттерн** с MediatR для разделения команд и запросов
- **FluentValidation** для декларативной валидации
- **AutoMapper** для автоматического маппинга DTO
- **Global Exception Handling** с форматом ProblemDetails
- **CORS** конфигурация для кросс-доменных запросов
- **Background Services** для очистки устаревших данных

---

## 🏗️ Архитектура

Проект реализован с использованием **Clean Architecture** и следует принципам **SOLID**:

```
SmartPlanner/
├── 📁 API/                    # ASP.NET Core Web API entry point
│   ├── Controllers/          # REST API контроллеры
│   ├── Hubs/                 # SignalR hubs
│   ├── Filters/              # Action filters (RateLimit, Exception)
│   ├── Middleware/           # Custom middleware
│   └── Configuration/        # DI и настройки
│
├── 📁 Application/           # Бизнес-логика (Domain Layer)
│   ├── Common/               # Общие интерфейсы, DTO, behaviors
│   ├── Auth/                 # Аутентификация и авторизация
│   ├── Goals/                # Логика управления целями
│   ├── Challenges/           # Логика челленджей
│   ├── Achievements/         # Система достижений
│   ├── Users/                # Управление пользователями
│   ├── Security/             # Аудит и безопасность
│   └── Services/             # Прикладные сервисы
│
├── 📁 Domain/                # Ядро домена (без зависимостей)
│   ├── Entities/             # Доменные сущности
│   ├── Enums/                # Перечисления
│   └── Exceptions/           # Доменные исключения
│
├── 📁 Infrastructure/        # Внешние зависимости
│   ├── Data/                 # EF Core DbContext и миграции
│   ├── Services/             # Реализации внешних сервисов
│   └── AI/                   # AI-рекомендации (опционально)
│
└── 📁 Tests/                 # Unit и интеграционные тесты
    ├── Application.Tests/
    ├── Domain.Tests/
    └── Infrastructure.Tests/
```

### 🔄 Паттерны и практики

| Паттерн | Применение |
|---------|-----------|
| **CQRS** | Разделение команд (Commands) и запросов (Queries) через MediatR |
| **Repository** | Абстракция доступа к данным через `IApplicationDbContext` |
| **Unit of Work** | Управление транзакциями через EF Core |
| **Factory** | Создание валидаторов и мапперов |
| **Strategy** | Обработка разных типов достижений и челленджей |
| **Pipeline Behavior** | Валидация, логирование, обработка ошибок в MediatR |

---

## 🚀 Быстрый старт

### 📋 Предварительные требования

- [.NET 9 SDK](https://dotnet.microsoft.com/download) или новее
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [Docker](https://www.docker.com/) (опционально, для контейнеризации)

### ⚙️ Настройка окружения

1. **Клонируйте репозиторий:**
```bash
git clone https://github.com/fairwix/SmartPlanner.git
cd SmartPlanner
```

2. **Настройте строку подключения** в `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=smartplanner;Username=postgres;Password=your_password"
  }
}
```

3. **Примените миграции базы данных:**
```bash
cd SmartPlanner.API
dotnet ef database update
```

4. **Запустите приложение:**
```bash
dotnet run --project SmartPlanner.API
```

5. **Откройте Swagger UI:**
```
http://localhost:5047/swagger
```

### 🐳 Запуск через Docker (опционально)

```bash
# Сборка и запуск
docker-compose up -d

# Просмотр логов
docker-compose logs -f api
```

---

## 🔐 Аутентификация и авторизация

### 🔄 Токены доступа

| Токен | Время жизни | Назначение |
|-------|------------|-----------|
| **Access Token** | 15 минут | Доступ к защищённым эндпоинтам |
| **Refresh Token** | 7 дней | Получение новых access-токенов |

### 📡 Авторизация в запросах

```http
Authorization: Bearer <your_access_token>
```

### 🔑 Регистрация и вход

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "username": "johndoe",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

```http
POST /api/auth/login
Content-Type: application/json

{
  "emailOrUsername": "johndoe",
  "password": "SecurePass123!"
}
```

### 🔄 Обновление токена

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "accessToken": "<expired_token>",
  "refreshToken": "<refresh_token>"
}
```

### 🛡️ Политики авторизации

| Политика | Описание |
|----------|---------|
| `AdminOnly` | Доступ только для роли `Admin` |
| `ResourceOwner` | Только владелец ресурса может редактировать |
| `RequireEmailConfirmed` | Требуется подтверждённый email |
| `CanManageUsers` | Управление пользователями |

---

## 📡 API Endpoints

### 👤 Аутентификация (`/api/auth`)

| Метод | Эндпоинт | Описание | Доступ |
|-------|----------|----------|--------|
| `POST` | `/register` | Регистрация нового пользователя | Публичный |
| `POST` | `/login` | Вход в систему | Публичный |
| `POST` | `/logout` | Выход из системы | 🔐 Авторизованный |
| `POST` | `/refresh` | Обновление токенов | Публичный |
| `POST` | `/revoke` | Отзыв refresh-токена | 🔐 Авторизованный |
| `POST` | `/forgot-password` | Запрос сброса пароля | Публичный |
| `POST` | `/reset-password` | Установка нового пароля | Публичный |
| `POST` | `/change-password` | Смена пароля | 🔐 Авторизованный |
| `GET` | `/profile` | Получение профиля | 🔐 Авторизованный |
| `GET` | `/confirm-email` | Подтверждение email | Публичный |

### 🎯 Цели (`/api/goals`)

| Метод | Эндпоинт | Описание | Доступ |
|-------|----------|----------|--------|
| `GET` | `/` | Список целей с пагинацией и фильтрами | 🔐 Авторизованный |
| `GET` | `/{id}` | Получение цели по ID | 🔐 Авторизованный |
| `POST` | `/` | Создание новой цели | 🔐 Авторизованный |
| `PUT` | `/{id}` | Обновление цели | 🔐 Владелец |
| `DELETE` | `/{id}` | Удаление цели | 🔐 Владелец |
| `POST` | `/{id}/progress` | Обновление прогресса | 🔐 Владелец |
| `POST` | `/bulk` | Массовое создание целей | 🔐 Авторизованный |
| `PUT` | `/bulk` | Массовое обновление целей | 🔐 Авторизованный |
| `DELETE` | `/bulk` | Массовое удаление целей | 🔐 Авторизованный |

### 🏆 Челленджи (`/api/challenges`)

| Метод | Эндпоинт | Описание | Доступ |
|-------|----------|----------|--------|
| `GET` | `/` | Список челленджей с фильтрами | 🔐 Авторизованный |
| `GET` | `/{id}` | Получение челленджа | 🔐 Авторизованный |
| `POST` | `/` | Создание челленджа | 🔐 Авторизованный |
| `POST` | `/{id}/join` | Присоединение к челленджу | 🔐 Авторизованный |
| `POST` | `/{id}/leave` | Выход из челленджа | 🔐 Участник |
| `POST` | `/{id}/progress` | Обновление прогресса | 🔐 Участник |

### 🏅 Достижения (`/api/achievements`)

| Метод | Эндпоинт | Описание | Доступ |
|-------|----------|----------|--------|
| `GET` | `/` | Список всех достижений | Публичный |
| `GET` | `/user/{userId}` | Достижения пользователя | 🔐 Владелец/Админ |
| `POST` | `/check` | Проверка и награждение | 🔐 Авторизованный |

### 📁 Файлы (`/api/files`)

| Метод | Эндпоинт | Описание | Доступ |
|-------|----------|----------|--------|
| `POST` | `/upload` | Загрузка файла (до 50 МБ) | 🔐 Авторизованный |
| `POST` | `/upload-multiple` | Загрузка нескольких файлов | 🔐 Авторизованный |
| `POST` | `/upload/chunked/start` | Начало чанковой загрузки | 🔐 Авторизованный |
| `POST` | `/upload/chunked` | Загрузка чанка | 🔐 Авторизованный |
| `GET` | `/upload/{id}/progress` | Прогресс загрузки | 🔐 Авторизованный |
| `POST` | `/check-duplicate` | Проверка дубликата по хешу | 🔐 Авторизованный |
| `GET` | `/{id}` | Скачивание файла | 🔐/🌐 В зависимости от доступа |
| `GET` | `/{id}/stream` | Потоковое скачивание | 🔐/🌐 В зависимости от доступа |
| `GET` | `/{id}/thumbnail` | Получение thumbnail | 🔐/🌐 В зависимости от доступа |
| `GET` | `/{id}/info` | Метаданные файла | 🔐/🌐 В зависимости от доступа |
| `DELETE` | `/{id}` | Удаление файла | 🔐 Владелец/Админ |
| `GET` | `/` | Список файлов пользователя | 🔐 Авторизованный |

### 👥 Пользователи (`/api/users`)

| Метод | Эндпоинт | Описание | Доступ |
|-------|----------|----------|--------|
| `GET` | `/{id}` | Получение профиля | 🔐 Владелец/Админ |
| `POST` | `/` | Создание пользователя | 🔐 Админ |
| `PUT` | `/{id}` | Обновление профиля | 🔐 Владелец |
| `PATCH` | `/{id}/block` | Блокировка пользователя | 🔐 Админ |
| `PATCH` | `/{id}/unblock` | Разблокировка пользователя | 🔐 Админ |
| `DELETE` | `/{id}` | Удаление пользователя | 🔐 Админ |

### 🔍 Админ-панель (`/api/admin`)

| Метод | Эндпоинт | Описание | Доступ |
|-------|----------|----------|--------|
| `GET` | `/audit` | Логи аудита безопасности | 🔐 Админ |
| `GET` | `/audit/user/{userId}` | Аудит конкретного пользователя | 🔐 Админ |
| `GET` | `/audit/suspicious` | Подозрительная активность | 🔐 Админ |
| `GET` | `/audit/summary` | Сводка по аудиту | 🔐 Админ |
| `POST` | `/achievements/award/{userId}/{achievementId}` | Награждение достижением | 🔐 Админ |

---

## 🔔 Real-time уведомления (SignalR)

### 📡 Подключение к хабам

```javascript
// NotificationHub - уведомления
const notificationHub = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/notifications?access_token=YOUR_TOKEN")
  .build();

// FileHub - файловые операции
const fileHub = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/file?access_token=YOUR_TOKEN")
  .build();
```

### 📤 Методы хабов

#### NotificationHub

| Метод | Параметры | Описание |
|-------|-----------|----------|
| `Ping()` | — | Проверка соединения |
| `SendTestNotification()` | — | Отправка тестового уведомления |
| `GetConnectionInfo()` | — | Информация о подключении |
| `GetHubStats()` | — | Статистика хаба (только админ) |
| `SubscribeToGroup(groupName)` | `string` | Подписка на группу |
| `UnsubscribeFromGroup(groupName)` | `string` | Отписка от группы |

#### FileHub

| Метод | Параметры | Описание |
|-------|-----------|----------|
| `StartTrackingUpload()` | `uploadId, fileName, fileSize, totalChunks` | Начало отслеживания |
| `UpdateUploadProgress()` | `uploadId, uploadedChunks, status?` | Обновление прогресса |
| `GetUploadProgress()` | `uploadId` | Получение прогресса |
| `NotifyFileDownload()` | `fileId, fileName` | Уведомление о скачивании |
| `NotifyFileUploadCompleted()` | `fileId, fileName, fileSize, isDuplicate` | Завершение загрузки |
| `SubscribeToFileEvents()` | `fileId` | Подписка на события файла |
| `GetActiveUploads()` | — | Список активных загрузок |
| `CancelUpload()` | `uploadId` | Отмена загрузки |

### 🔔 События клиента

#### INotificationClient
```typescript
interface INotificationClient {
  Connected(connectionInfo: any): Promise<void>;
  Disconnected(message: string): Promise<void>;
  ReceiveNotification(notification: any): Promise<void>;
  // ... другие события
}
```

#### IFileClient
```typescript
interface IFileClient {
  UploadStarted(uploadInfo: any): Promise<void>;
  UploadProgressUpdated(progress: any): Promise<void>;
  UploadCompleted(completion: any): Promise<void>;
  // ... другие события
}
```

---

## 📁 Работа с файлами

### 📤 Загрузка файла (простая)

```http
POST /api/files/upload
Content-Type: multipart/form-data

file: <binary_file>
isPublic: true
expiresAt: 2024-12-31T23:59:59Z
```

### 🔄 Chunked Upload (для больших файлов)

```http
# 1. Начало загрузки
POST /api/files/upload/chunked/start
Content-Type: application/json

{
  "fileName": "large_video.mp4",
  "fileSize": 1073741824,
  "totalChunks": 100,
  "fileHash": "sha256_hash_here",
  "isPublic": false
}

# 2. Загрузка чанков (повторять для каждого)
POST /api/files/upload/chunked
Content-Type: multipart/form-data

chunk: <binary_chunk>
uploadId: "upload_abc123"
chunkIndex: 0
totalChunks: 100
fileName: "large_video.mp4"

# 3. Проверка прогресса
GET /api/files/upload/upload_abc123/progress

# 4. Завершение (автоматически после последнего чанка)
```

### 🎨 Обработка изображений

При загрузке изображений автоматически:
- ✅ Извлекаются метаданные (размеры, EXIF)
- ✅ Удаляются приватные EXIF-данные для публичных файлов
- ✅ Генерируются thumbnail (small: 200x200, medium: 800x600)
- ✅ Оптимизируется качество (85% по умолчанию)

### 🔍 Проверка дубликатов

```http
POST /api/files/check-duplicate
Content-Type: application/json

{
  "fileHash": "sha256_hash",
  "fileName": "document.pdf",
  "fileSize": 1048576
}
```

**Ответ при дубликате:**
```json
{
  "isDuplicate": true,
  "existingFileId": "guid_here",
  "fileName": "existing_name.pdf",
  "fileSize": 1048576,
  "uploadedAt": "2024-01-01T00:00:00Z"
}
```

---

## 🧪 Тестирование

### ▶️ Запуск тестов

```bash
# Все тесты
dotnet test

# Только юнит-тесты
dotnet test --filter "TestCategory=Unit"

# Тесты с покрытием
dotnet test --collect:"XPlat Code Coverage"
```

### 📊 Покрытие кода

Проект включает **85%+ покрытие** юнит-тестами для критических компонентов:

- ✅ Валидаторы команд и запросов
- ✅ Обработчики CQRS (Handlers)
- ✅ Сервисы (FileService, EmailService, TokenService)
- ✅ Доменные сущности и бизнес-правила
- ✅ Авторизационные хендлеры

### 🧪 Пример теста

```csharp
[Fact]
public async Task UploadFileAsync_ValidFile_UploadsSuccessfully()
{
    // Arrange
    var userId = Guid.NewGuid();
    var mockFile = CreateMockFormFile("test.jpg", 1024, "image/jpeg");
    
    // Act
    var result = await _service.UploadFileAsync(mockFile.Object, userId);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("test.jpg", result.OriginalFileName);
    _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}
```

---

## ⚙️ Конфигурация

### 📄 appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=smartplanner;Username=postgres;Password=***"
  },
  "JwtSettings": {
    "Secret": "your-256-bit-secret-key-here",
    "Issuer": "SmartPlanner",
    "Audience": "SmartPlannerClients",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "AppSettings": {
    "BaseUrl": "https://api.smartplanner.com",
    "FrontendUrls": {
      "LoginUrl": "/login",
      "DashboardUrl": "/dashboard"
    }
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "noreply@smartplanner.com",
    "SenderName": "Smart Planner",
    "UseSsl": true,
    "RequiresAuthentication": true,
    "Username": "***",
    "Password": "***"
  },
  "RateLimiting": {
    "EnableRateLimiting": true,
    "PermitLimit": 10,
    "WindowSeconds": 60
  }
}
```

### 🌍 Переменные окружения

```bash
# База данных
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=smartplanner
POSTGRES_USER=postgres
POSTGRES_PASSWORD=***

# JWT
JWT_SECRET=your-secret-key
JWT_ISSUER=SmartPlanner
JWT_AUDIENCE=SmartPlannerClients

# Email
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=***
SMTP_PASS=***

# Приложение
ASPNETCORE_ENVIRONMENT=Production
APP_BASE_URL=https://api.smartplanner.com
```

---

## 🛡️ Безопасность

### 🔐 Меры защиты

| Угроза | Мера защиты |
|--------|------------|
| **SQL Injection** | Parameterized queries через EF Core |
| **XSS** | Санитизация входных данных, Content Security Policy |
| **CSRF** | JWT в Authorization header (не в cookies) |
| **Brute Force** | Rate Limiting, блокировка после неудачных попыток |
| **Token Theft** | Refresh token rotation, короткое время жизни access token |
| **File Upload Attacks** | Валидация MIME-type, проверка сигнатур, ограничение размера |
| **Data Exposure** | Контроль доступа на уровне приложения, шифрование чувствительных данных |

### 🔍 Аудит безопасности

Все критические события логируются в таблицу `SecurityAuditLogs`:

```csharp
public enum SecurityEventType
{
    Login, FailedLogin, Logout, TokenRefresh,
    Register, EmailConfirmed, PasswordReset,
    UserBlocked, AccessDenied, SuspiciousActivity
    // ... и другие
}
```

**Пример записи аудита:**
```json
{
  "eventType": "FailedLogin",
  "userId": "guid",
  "email": "user@example.com",
  "ipAddress": "192.168.1.1",
  "userAgent": "Mozilla/5.0...",
  "success": false,
  "details": { "Reason": "Invalid password" },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## 📊 Мониторинг и логирование

### 🪵 Структура логов

```json
{
  "Timestamp": "2024-01-15T10:30:00Z",
  "Level": "Information",
  "Category": "SmartPlanner.API.Controllers.GoalsController",
  "Message": "Goal {GoalId} created for user {UserId}",
  "Properties": {
    "GoalId": "guid",
    "UserId": "guid",
    "SourceContext": "SmartPlanner.API.Controllers.GoalsController"
  }
}
```

### 📈 Health Checks

```http
GET /health
```

**Ответ:**
```json
{ "status": "Healthy" }
```

### 🔧 Конфигурация логирования (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "SmartPlanner": "Debug"
    },
    "Console": {
      "FormatterName": "simple",
      "FormatterOptions": {
        "TimestampFormat": "yyyy-MM-dd HH:mm:ss "
      }
    }
  }
}
```
