using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.Application.Dtos.Files;
using SmartPlanner.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.WebUtilities;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.API.Attributes;
using SmartPlanner.API.Helpers;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.API.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using SmartPlanner.API.Filters;
using SmartPlanner.API.Hubs;

namespace SmartPlanner.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FilesController> _logger;
        private readonly IFileNotificationService _notificationService;

        public FilesController(
            IFileService fileService,
            IFileNotificationService notificationService,
            ILogger<FilesController> logger)
        {
            _fileService = fileService;
            _notificationService = notificationService;
            _logger = logger;
        }

        #region Загрузка файлов

        /// <summary>
        /// Загрузить один файл
        /// </summary>
        /// <param name="file">Файл для загрузки</param>
        /// <param name="isPublic">Публичный доступ</param>
        /// <param name="expiresAt">Время истечения (необязательно)</param>
        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)] // 50MB
        [Consumes("multipart/form-data")]
        [RateLimit("file:upload", limit: 10, seconds: 60)]
        [ProducesResponseType(typeof(FileMetadataDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<FileMetadataDto>> UploadFile(
            IFormFile file,
            bool isPublic = false,
            DateTime? expiresAt = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                _logger.LogInformation("Пользователь {UserId} загружает файл: {FileName} ({Size} bytes), IsPublic: {IsPublic}",
                    userId, file.FileName, file.Length, isPublic);

                // Валидация происходит в FileService
                var result = await _fileService.UploadFileAsync(
                    file, userId, isPublic, expiresAt);

                await _notificationService.NotifyFileUploadedAsync(
                    userId.ToString(),
                    result.OriginalFileName,
                    result.Size,
                    result.Id);

                _logger.LogInformation("Файл {FileId} успешно загружен пользователем {UserId}",
                    result.Id, userId);

                await _notificationService.NotifyFileUploadedAsync(
                    userId.ToString(),
                    result.OriginalFileName,
                    result.Size,
                    result.Id,
                    false); // isDuplicate = false (можно определить из result)

                // Возвращаем DTO с URL как в ТЗ
                var response = new
                {
                    result.Id,
                    OriginalFileName = result.OriginalFileName,
                    Size = result.Size,
                    Url = $"/api/files/{result.Id}",
                    result.IsPublic,
                    result.ExpiresAt,
                    UploadedAt = result.CreatedAt
                };

                try
                {
                    await _notificationService.NotifyFileUploadedAsync(
                        userId.ToString(),
                        result.OriginalFileName,
                        result.Size,
                        result.Id,
                        false); // isDuplicate = false
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning(notifEx, "Не удалось отправить уведомление о загрузке файла {FileId}", result.Id);
                    // Не прерываем основной поток из-за уведомления
                }

                // Возвращаем DTO с URL как в ТЗ
                var resp = new
                {
                    result.Id,
                    OriginalFileName = result.OriginalFileName,
                    Size = result.Size,
                    Url = $"/api/files/{result.Id}",
                    result.IsPublic,
                    result.ExpiresAt,
                    UploadedAt = result.CreatedAt
                };

                return CreatedAtAction(nameof(GetFileInfo), new { id = result.Id }, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Ошибка валидации файла");
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Ошибка доступа при загрузке файла");
                return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неожиданная ошибка при загрузке файла");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Внутренняя ошибка сервера" });
            }
        }
        [HttpPost("test-notification-simple")]
        [Authorize]
        public async Task<ActionResult> TestNotificationSimple()
        {
            try
            {
                var userId = GetCurrentUserId().ToString();

                // 1. Проверяем подключения
                var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<NotificationHub>>();

                // 2. Отправляем тестовое уведомление
                await hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", new
                {
                    Id = Guid.NewGuid(),
                    Title = "ТЕСТ из контроллера",
                    Message = "Если видишь это - SignalR работает! 🎉",
                    Type = "success",
                    Timestamp = DateTime.UtcNow
                });

                return Ok(new
                {
                    success = true,
                    message = "Тестовое уведомление отправлено",
                    userId = userId,
                    group = $"user_{userId}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка теста уведомлений");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Загрузить несколько файлов
        /// </summary>
        /// <param name="files">Список файлов</param>
        /// <param name="isPublic">Публичный доступ для всех файлов</param>
        /// <param name="expiresAt">Время истечения для всех файлов (необязательно)</param>
        [HttpPost("upload-multiple")]
        [RequestSizeLimit(100_000_000)] // 100MB
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<object>>> UploadMultipleFiles(
            List<IFormFile> files,
            bool isPublic = false,
            DateTime? expiresAt = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                // Проверка количества файлов
                if (files == null || files.Count == 0)
                    return BadRequest(new { error = "Не предоставлены файлы для загрузки" });

                if (files.Count > 10)
                    return BadRequest(new { error = "Максимальное количество файлов за раз: 10" });

                _logger.LogInformation("Пользователь {UserId} загружает {Count} файлов",
                    userId, files.Count);

                // Транзакционная обработка (если один файл не проходит валидацию - отменяем все)
                var results = await _fileService.UploadMultipleFilesAsync(
                    files, userId, isPublic, expiresAt);

                _logger.LogInformation("Успешно загружено {SuccessCount} из {TotalCount} файлов для пользователя {UserId}",
                    results.Count, files.Count, userId);

                foreach (var result in results)
                {
                    try
                    {
                        await _notificationService.NotifyFileUploadedAsync(
                            userId.ToString(),
                            result.OriginalFileName,
                            result.Size,
                            result.Id,
                            false);
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogWarning(notifEx,
                            "Не удалось отправить уведомление для файла {FileId}",
                            result.Id);
                    }
                }


                var response = results.Select(result => new
                {
                    result.Id,
                    OriginalFileName = result.OriginalFileName,
                    Size = result.Size,
                    Url = $"/api/files/{result.Id}",
                    result.IsPublic,
                    result.ExpiresAt,
                    UploadedAt = result.CreatedAt
                }).ToList();

                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Ошибка валидации файлов при множественной загрузке");
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Ошибка доступа при множественной загрузке файлов");
                return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неожиданная ошибка при множественной загрузке файлов");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Внутренняя ошибка сервера" });
            }
        }



[HttpPost("upload/chunked/start")]
[Authorize]
[RequestSizeLimit(1_000_000)] // 1MB для метаданных
public async Task<ActionResult<ChunkedUploadProgressResponseDto>> StartChunkedUpload(
    [FromBody] ChunkedUploadStartDto request)
{
    try
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _fileService.StartChunkedUploadAsync(request, userId);

        // Если найден дубликат - сразу возвращаем информацию о нем
        if (result.IsDuplicate)
        {
            return Ok(new
            {
                result.UploadId,
                result.Progress,
                result.Status,
                IsDuplicate = true,
                ExistingFileId = result.ExistingFileId,
                Message = result.Message,
                Url = result.ExistingFileId.HasValue
                    ? Url.Action("Download", "Files", new { id = result.ExistingFileId.Value }, Request.Scheme)
                    : null
            });
        }

        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при начале чанковой загрузки");
        return StatusCode(500, new { error = ex.Message });
    }
}

[HttpGet("upload/{uploadId}/progress")]
[Authorize]
public async Task<ActionResult<ChunkedUploadProgressDto>> GetUploadProgress(string uploadId)
{
    try
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var progress = await _fileService.GetUploadProgressAsync(uploadId, userId);

        if (progress == null)
            return NotFound(new { error = "Загрузка не найдена или завершена" });

        return Ok(progress);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при получении прогресса загрузки {UploadId}", uploadId);
        return StatusCode(500, new { error = ex.Message });
    }
}

[HttpPost("check-duplicate")]
[Authorize]
public async Task<ActionResult<CheckDuplicateResponseDto>> CheckDuplicate(
    [FromBody] CheckDuplicateRequestDto request)
{
    try
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        if (string.IsNullOrEmpty(request.FileHash))
            return BadRequest(new { error = "Хеш файла обязателен" });

        var result = await _fileService.CheckDuplicateAsync(request, userId);

        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при проверке дубликата файла");
        return StatusCode(500, new { error = ex.Message });
    }
}

// Модифицируем существующий UploadChunked метод
[HttpPost("upload/chunked")]
[Authorize]
[RequestSizeLimit(50_000_000)]
[Consumes("multipart/form-data")]
public async Task<ActionResult> UploadChunked([FromForm] ChunkedUploadDto dto)
{
    try
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        // Проверяем, не является ли это дубликатной сессией
        var progress = await _fileService.GetUploadProgressAsync(dto.UploadId, userId);
        if (progress?.Status == "completed")
        {
            return Ok(new
            {
                IsComplete = true,
                Progress = 100,
                Message = "Файл уже загружен"
            });
        }

        // Загружаем чанк
        var result = await _fileService.UploadChunkAsync(
            dto.Chunk,
            dto.UploadId,
            dto.ChunkIndex,
            dto.TotalChunks,
            dto.FileName,
            userId,
            dto.IsPublic ?? false,
            dto.ExpiresAt);

        // Если все чанки загружены
        if (result.ChunksReceived == result.TotalChunks && result.Status == "assembling")
        {
            // Завершаем загрузку
            var fileMetadata = await _fileService.CompleteChunkedUploadAsync(
                dto.UploadId, userId);

            return Ok(new
            {
                IsComplete = true,
                FileId = fileMetadata.Id,
                FileName = fileMetadata.OriginalFileName,
                Size = fileMetadata.Size,
                Hash = fileMetadata.Hash,
                Url = Url.Action("Download", "Files", new { id = fileMetadata.Id }, Request.Scheme),
                Message = "Файл успешно загружен"
            });
        }

        return Ok(new
        {
            IsComplete = false,
            Progress = result.Progress,
            ChunksReceived = result.ChunksReceived,
            TotalChunks = result.TotalChunks,
            Status = result.Status,
            Message = $"Загружено {result.ChunksReceived} из {result.TotalChunks} частей"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при загрузке чанка для {UploadId}", dto.UploadId);
        return StatusCode(500, new { error = ex.Message });
    }
}

    /// <summary>
/// Получить thumbnail изображения
/// </summary>
/// <param name="id">Идентификатор файла</param>
/// <param name="size">Размер (small, medium, large)</param>
/// <param name="width">Кастомная ширина (опционально)</param>
/// <param name="height">Кастомная высота (опционально)</param>
/// <param name="crop">Обрезать изображение</param>
[HttpGet("{id}/thumbnail")]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> GetThumbnail(
    Guid id,
    [FromQuery] string size = "small",
    [FromQuery] int? width = null,
    [FromQuery] int? height = null,
    [FromQuery] bool crop = false)
{
    try
    {
        _logger.LogInformation("Запрос thumbnail для файла {FileId}, size: {Size}", id, size);

        // Получаем информацию о файле
        var currentUserId = GetCurrentUserId();
        Guid? userId = currentUserId != Guid.Empty ? currentUserId : null;

        var fileInfo = await _fileService.GetFileMetadataAsync(id, userId);
        if (fileInfo == null)
            return NotFound(new { error = "Файл не найден" });

        // Проверяем права доступа
        var isOwner = fileInfo.UploadedById == currentUserId;
        var isAdmin = User.IsInRole("Admin");
        var isPublic = fileInfo.IsPublic;

        if (!isPublic && !isOwner && !isAdmin)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Доступ запрещен" });

        // Проверяем, что это изображение
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", fileInfo.Path);
        if (!System.IO.File.Exists(fullPath))
            return NotFound(new { error = "Файл не найден на диске" });

        // Проверяем, что файл является изображением
        var extension = Path.GetExtension(fileInfo.OriginalFileName).ToLowerInvariant();
        var isImage = extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" => true,
            _ => false
        };

        if (!isImage)
            return BadRequest(new { error = "Файл не является изображением" });

        // Определяем размер thumbnail
        ThumbnailSize thumbnailSize = size.ToLower() switch
        {
            "small" => ThumbnailSize.Small,
            "medium" => ThumbnailSize.Medium,
            "large" => ThumbnailSize.Large,
            _ => ThumbnailSize.Small
        };

        // Получаем или генерируем thumbnail
        var thumbnailPath = await GetOrCreateThumbnailAsync(
            fullPath, fileInfo, thumbnailSize, width, height, crop);

        // Устанавливаем заголовки для кэширования
        Response.Headers.Append("Cache-Control", "public, max-age=31536000"); // 1 год
        Response.Headers.Append("ETag", $"\"{fileInfo.Hash}\"");
        Response.Headers.Append("Last-Modified", fileInfo.CreatedAt.ToString("R"));

        // Определяем Content-Type
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/jpeg"
        };

        // Возвращаем thumbnail
        var fileStream = new FileStream(thumbnailPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        return File(fileStream, contentType, $"{Path.GetFileNameWithoutExtension(fileInfo.OriginalFileName)}_thumb{extension}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при получении thumbnail для файла {FileId}", id);
        return StatusCode(StatusCodes.Status500InternalServerError,
            new { error = "Внутренняя ошибка сервера" });
    }
}

private async Task<string> GetOrCreateThumbnailAsync(
    string sourceImagePath,
    FileMetadataDto fileInfo,
    ThumbnailSize size,
    int? customWidth,
    int? customHeight,
    bool crop)
{
    var directory = Path.GetDirectoryName(sourceImagePath)!;
    var fileName = Path.GetFileNameWithoutExtension(sourceImagePath);
    var extension = Path.GetExtension(sourceImagePath);

    // Формируем имя файла для thumbnail
    string thumbnailFileName;
    if (customWidth.HasValue || customHeight.HasValue)
    {
        var width = customWidth ?? (size == ThumbnailSize.Small ? 200 : 800);
        var height = customHeight ?? (size == ThumbnailSize.Small ? 200 : 600);
        thumbnailFileName = $"{fileName}_{width}x{height}{(crop ? "_crop" : "")}{extension}";
    }
    else
    {
        var (width, height) = size switch
        {
            ThumbnailSize.Small => (200, 200),
            ThumbnailSize.Medium => (800, 600),
            ThumbnailSize.Large => (fileInfo.Width ?? 1920, fileInfo.Height ?? 1080),
            _ => (200, 200)
        };
        thumbnailFileName = $"{fileName}_{width}x{height}{(crop ? "_crop" : "")}{extension}";
    }

    var thumbnailPath = Path.Combine(directory, thumbnailFileName);

    // // Если thumbnail уже существует - возвращаем его
    // if (File.Exists(thumbnailPath))
    //     return thumbnailPath;

    // Иначе генерируем новый thumbnail
    try
    {
        // Используем ImageService через рефлексию или добавим в контроллер
        // Пока просто сгенерируем через статический вызов
        var imageService = HttpContext.RequestServices.GetService<IImageService>();
        if (imageService != null)
        {
            if (customWidth.HasValue || customHeight.HasValue)
            {
                var width = customWidth ?? 200;
                var height = customHeight ?? 200;
                thumbnailPath = await imageService.GenerateThumbnailAsync(
                    sourceImagePath, directory, width, height, crop);
            }
            else
            {
                thumbnailPath = await imageService.GenerateThumbnailAsync(
                    sourceImagePath, directory, size, crop);
            }
        }
        else
        {
            // Fallback: возвращаем оригинал если сервис недоступен
            thumbnailPath = sourceImagePath;
        }

        return thumbnailPath;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при генерации thumbnail: {Path}", sourceImagePath);
        // В случае ошибки возвращаем оригинал
        return sourceImagePath;
    }

}


[HttpPost("upload-stream")]
[Authorize]
[RequestSizeLimit(2_000_000_000)] // 2GB
[Consumes("multipart/form-data")]
public async Task<IActionResult> UploadStream(
    IFormFile file,
    [FromQuery] bool isPublic = false,
    [FromQuery] DateTime? expiresAt = null,
    CancellationToken cancellationToken = default)
{
    try
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        _logger.LogInformation("Начало стриминговой загрузки файла пользователем {UserId}", userId);

        // Используем новый метод для больших файлов
        var result = await _fileService.UploadLargeFileAsync(
            file, userId, isPublic, expiresAt, cancellationToken);

        _logger.LogInformation("Стриминговая загрузка завершена: {FileId}", result.Id);

        return CreatedAtAction(
            nameof(GetFileInfo),
            new { id = result.Id },
            new
            {
                id = result.Id,
                fileName = result.FileName,
                originalFileName = result.OriginalFileName,
                size = result.Size,
                contentType = result.ContentType,
                url = $"/api/Files/{result.Id}",
                thumbnailUrl = result.ContentType.StartsWith("image/")
                    ? $"/api/Files/{result.Id}/thumbnail?size=small"
                    : null,
                isPublic = result.IsPublic,
                expiresAt = result.ExpiresAt,
                uploadedAt = result.CreatedAt
            });
    }
    catch (OperationCanceledException)
    {
        _logger.LogInformation("Загрузка отменена пользователем");
        return StatusCode(499);
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "Ошибка валидации файла");
        return BadRequest(new { error = ex.Message });
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Ошибка доступа");
        return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при стриминговой загрузке файла");
        return StatusCode(StatusCodes.Status500InternalServerError,
            new { error = "Внутренняя ошибка сервера" });
    }
}



        #endregion

        #region Получение файлов

        /// <summary>
        /// Получить список файлов пользователя
        /// </summary>
        /// <param name="contentType">Фильтр по типу контента (image/*, application/pdf и т.д.)</param>
        /// <param name="search">Поиск по имени файла</param>
        /// <param name="sortBy">Поле для сортировки (createdAt, size, fileName)</param>
        /// <param name="sortOrder">Порядок сортировки (asc, desc)</param>
        /// <param name="page">Номер страницы (по умолчанию 1)</param>
        /// <param name="pageSize">Размер страницы (по умолчанию 10, максимум 50)</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<FileMetadataDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PagedResult<FileMetadataDto>>> GetFiles(
            [FromQuery] string? contentType = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                var isAdmin = User.IsInRole("Admin");

                _logger.LogDebug("Получение файлов пользователем {UserId} (Admin: {IsAdmin})",
                    userId, isAdmin);

                // TODO: Для администраторов нужно реализовать отдельный метод в сервисе
                // который возвращает все файлы, а не только пользовательские

                var parameters = new FileQueryParameters
                {
                    Page = Math.Max(1, page),
                    PageSize = Math.Min(Math.Max(1, pageSize), 50), // Ограничиваем 50
                    Search = search,
                    ContentType = contentType,
                    SortBy = sortBy,
                    SortDescending = sortOrder?.ToLower() == "desc"
                };

                var result = await _fileService.GetUserFilesAsync(userId, parameters);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Ошибка доступа при получении списка файлов");
                return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка файлов");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Скачать файл
        /// </summary>
        /// <param name="id">Идентификатор файла</param>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        public async Task<IActionResult> DownloadFile(Guid id)
        {
            try
            {
                _logger.LogInformation("Запрос на скачивание файла {FileId}", id);

                // Получаем текущего пользователя (может быть null для анонимных)
                var currentUserId = GetCurrentUserId();
                Guid? userId = currentUserId != Guid.Empty ? currentUserId : null;

                // Проверяем права доступа и получаем метаданные
                var fileInfo = await _fileService.GetFileMetadataAsync(id, userId);
                if (fileInfo == null)
                {
                    _logger.LogWarning("Файл {FileId} не найден", id);
                    return NotFound(new { error = "Файл не найден" });
                }

                // Проверяем права доступа (IsPublic, владелец, админ)
                var isOwner = fileInfo.UploadedById == currentUserId;
                var isAdmin = User.IsInRole("Admin");
                var isPublic = fileInfo.IsPublic;

                if (!isPublic && !isOwner && !isAdmin)
                {
                    _logger.LogWarning("Доступ к файлу {FileId} запрещен для пользователя {UserId}",
                        id, currentUserId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { error = "Доступ запрещен" });
                }

                // Проверяем срок действия
                if (fileInfo.ExpiresAt.HasValue && fileInfo.ExpiresAt.Value < DateTime.UtcNow)
                {
                    _logger.LogWarning("Срок действия файла {FileId} истёк", id);
                    return StatusCode(StatusCodes.Status410Gone,
                        new { error = "Срок действия файла истёк" });
                }

                // Скачиваем файл (метод уже увеличивает DownloadCount)
                var (stream, contentType, fileName) =
                    await _fileService.DownloadFileAsync(id, userId);

                _logger.LogInformation("Файл {FileId} скачивается, ContentType: {ContentType}",
                    id, contentType);

                if (currentUserId != Guid.Empty)
                {
                    try
                    {
                        await _notificationService.NotifyFileDownloadedAsync(
                            currentUserId.ToString(),
                            fileInfo.OriginalFileName,
                            id);
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogWarning(notifEx,
                            "Не удалось отправить уведомление о скачивании файла {FileId}",
                            id);
                    }
                }

                // Определяем Content-Disposition
                var isImage = contentType.StartsWith("image/");
                var isPdf = contentType == "application/pdf";
                var contentDisposition = isImage || isPdf ? "inline" : "attachment";

                // Устанавливаем заголовки
                Response.Headers.Append("Content-Disposition",
                    $"{contentDisposition}; filename=\"{fileName}\"");
                Response.Headers.Append("X-File-Id", id.ToString());
                Response.Headers.Append("X-File-Name", fileName);

                return File(stream, contentType);
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("Файл {FileId} не найден при скачивании", id);
                return NotFound(new { error = "Файл не найден" });
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Доступ к файлу {FileId} запрещен", id);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Доступ запрещен" });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("истёк"))
            {
                _logger.LogWarning("Срок действия файла {FileId} истёк", id);
                return StatusCode(StatusCodes.Status410Gone,
                    new { error = "Срок действия файла истёк" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при скачивании файла {FileId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Внутренняя ошибка сервера" });
            }
        }

        #region Тестирование уведомлений

/// <summary>
/// Тест отправки уведомления о файле (для демонстрации SignalR)
/// </summary>
[HttpPost("test-notification")]
[Authorize]
public async Task<ActionResult> TestFileNotification([FromBody] TestNotificationRequest request)
{
    try
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        _logger.LogInformation("🧪 Тест уведомления от пользователя {UserId}", userId);

        if (request.Action == "upload")
        {
            await _notificationService.NotifyFileUploadedAsync(
                userId.ToString(),
                request.FileName ?? "тестовый-файл.pdf",
                request.FileSize ?? 1024 * 1024,
                request.FileId ?? Guid.NewGuid(),
                false);
        }
        else if (request.Action == "delete")
        {
            await _notificationService.NotifyFileDeletedAsync(
                userId.ToString(),
                request.FileName ?? "тестовый-файл.pdf",
                request.FileId ?? Guid.NewGuid(),
                request.Reason ?? "тестовое удаление");
        }
        else if (request.Action == "download")
        {
            await _notificationService.NotifyFileDownloadedAsync(
                userId.ToString(),
                request.FileName ?? "тестовый-файл.pdf",
                request.FileId ?? Guid.NewGuid());
        }

        return Ok(new
        {
            success = true,
            message = $"Тестовое уведомление отправлено (действие: {request.Action})",
            userId = userId.ToString(),
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при тестировании уведомлений");
        return StatusCode(500, new { error = ex.Message });
    }
}

public class TestNotificationRequest
{
    public string Action { get; set; } = "upload"; // upload, delete, download
    public string? FileName { get; set; }
    public Guid? FileId { get; set; }
    public long? FileSize { get; set; }
    public string? Reason { get; set; }
}

#endregion

/// <summary>
/// Скачать файл
/// </summary>
/// <param name="id">Идентификатор файла</param>
[HttpGet("{id}/stream")]
[AllowAnonymous]
[ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status410Gone)]
public async Task<IActionResult> StreamFile(Guid id)
{
    try
    {
        _logger.LogInformation("Скачивание файла {FileId}", id);

        var currentUserId = GetCurrentUserId();
        Guid? userId = currentUserId != Guid.Empty ? currentUserId : null;

        var fileInfo = await _fileService.GetFileMetadataAsync(id, userId);
        if (fileInfo == null)
        {
            return NotFound(new { error = "Файл не найден" });
        }

        // Проверяем права доступа
        var isOwner = fileInfo.UploadedById == currentUserId;
        var isAdmin = User.IsInRole("Admin");
        var isPublic = fileInfo.IsPublic;

        if (!isPublic && !isOwner && !isAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { error = "Доступ запрещен" });
        }

        // Проверяем срок действия
        if (fileInfo.ExpiresAt.HasValue && fileInfo.ExpiresAt.Value < DateTime.UtcNow)
        {
            return StatusCode(StatusCodes.Status410Gone,
                new { error = "Срок действия файла истёк" });
        }

        // Получаем поток файла
        var (fileStream, contentType, fileName, fileSize) =
            await _fileService.GetFileStreamAsync(id, userId);

        try
        {
            // Увеличиваем счетчик скачиваний
            await _fileService.IncrementDownloadCountAsync(id, userId);

            // ВАЖНО: Возвращаем FileStreamResult, а не EmptyResult
            // Swagger UI покажет кнопку "Download file" для FileStreamResult
            return File(fileStream, contentType, fileName);
        }
        catch
        {
            await fileStream.DisposeAsync();
            throw;
        }
    }
    catch (FileNotFoundException)
    {
        return NotFound(new { error = "Файл не найден" });
    }
    catch (UnauthorizedAccessException)
    {
        return StatusCode(StatusCodes.Status403Forbidden,
            new { error = "Доступ запрещен" });
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("истёк"))
    {
        return StatusCode(StatusCodes.Status410Gone,
            new { error = "Срок действия файла истёк" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при скачивании файла {FileId}", id);
        return StatusCode(StatusCodes.Status500InternalServerError,
            new { error = "Внутренняя ошибка сервера" });
    }
}



    // Добавить этот метод в конец класса FilesController, перед GetCurrentUserId()
    private async Task IncreaseDownloadCountAsync(Guid fileId)
    {
        try
        {
            // Используем рефлексию или добавим метод в сервис
            // Пока просто логируем - в streaming не всегда нужно увеличивать счетчик
            _logger.LogDebug("Streaming файла {FileId} - счетчик не увеличен", fileId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось увеличить счетчик скачиваний для файла {FileId}", fileId);
        }
    }

        /// <summary>
        /// Получить метаданные файла без скачивания
        /// </summary>
        /// <param name="id">Идентификатор файла</param>
        [HttpGet("{id}/info")]
        [ProducesResponseType(typeof(FileMetadataDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<FileMetadataDto>> GetFileInfo(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                Guid? userIdParam = userId != Guid.Empty ? userId : null;

                var fileInfo = await _fileService.GetFileMetadataAsync(id, userIdParam);
                if (fileInfo == null)
                {
                    _logger.LogWarning("Файл {FileId} не найден", id);
                    return NotFound(new { error = "Файл не найден" });
                }

                // Проверяем права доступа
                var isOwner = fileInfo.UploadedById == userId;
                var isAdmin = User.IsInRole("Admin");
                var isPublic = fileInfo.IsPublic;

                if (!isPublic && !isOwner && !isAdmin)
                {
                    _logger.LogWarning("Доступ к метаданным файла {FileId} запрещен для пользователя {UserId}",
                        id, userId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { error = "Доступ запрещен" });
                }

                return Ok(fileInfo);
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("Файл {FileId} не найден", id);
                return NotFound(new { error = "Файл не найден" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Доступ к метаданным файла {FileId} запрещен", id);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Доступ запрещен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении метаданных файла {FileId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Внутренняя ошибка сервера" });
            }
        }

        #endregion

        #region Управление файлами

        /// <summary>
        /// Удалить файл
        /// </summary>
        /// <param name="id">Идентификатор файла</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteFile(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == Guid.Empty) return Unauthorized();

                _logger.LogInformation("Запрос на удаление файла {FileId} пользователем {UserId}",
                    id, userId);

                FileMetadataDto fileInfo = null;
                try
                {
                    fileInfo = await _fileService.GetFileMetadataAsync(id, userId);
                }
                catch
                {
                    // Игнорируем ошибки при получении метаданных
                }

                await _notificationService.NotifyFileDeletedAsync(
                    userId.ToString(),
                    fileInfo.FileName,
                    id,
                    "Удалено пользователем");

                var fileOwnerId = await _fileService.GetFileOwnerAsync(id);
                if (fileOwnerId == Guid.Empty)
                {
                    _logger.LogWarning("Файл {FileId} не найден", id);
                    return NotFound(new { error = "Файл не найден" });
                }

                // Проверяем права: владелец или админ
                var isAdmin = User.IsInRole("Admin");
                var isOwner = fileOwnerId == userId;

                if (!isOwner && !isAdmin)
                {
                    _logger.LogWarning("Пользователь {UserId} не имеет прав на удаление файла {FileId}",
                        userId, id);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { error = "Недостаточно прав для удаления файла" });
                }

                if (fileInfo != null)
                {
                    try
                    {
                        var reason = isAdmin && !isOwner ? "удален администратором" : "";
                        await _notificationService.NotifyFileDeletedAsync(
                            userId.ToString(),
                            fileInfo.OriginalFileName,
                            id,
                            reason);
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogWarning(notifEx,
                            "Не удалось отправить уведомление об удалении файла {FileId}",
                            id);
                    }
                }

                await _fileService.DeleteFileAsync(id, userId);

                _logger.LogInformation("Файл {FileId} успешно удалён пользователем {UserId}",
                    id, userId);

                return NoContent();
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("Файл {FileId} не найден", id);
                return NotFound(new { error = "Файл не найден" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Ошибка доступа при удалении файла {FileId}", id);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "Доступ запрещен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении файла {FileId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Внутренняя ошибка сервера" });
            }
        }

        #endregion

        #region Вспомогательные методы

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }


    #region Range Streaming Support

    private class RangeHelper
    {
        public long Start { get; set; }
        public long End { get; set; }
        public long Length { get; set; }
        public long TotalLength { get; set; }

        public static bool TryParse(string rangeHeader, long totalLength, out RangeHelper range)
        {
            range = null;

            if (string.IsNullOrEmpty(rangeHeader) || !rangeHeader.StartsWith("bytes="))
                return false;

            var rangeValue = rangeHeader.Substring("bytes=".Length);
            var ranges = rangeValue.Split('-');

            if (ranges.Length != 2)
                return false;

            long start, end;

            if (string.IsNullOrEmpty(ranges[0]))
            {
                // Формат: bytes=-500 (последние 500 байт)
                if (!long.TryParse(ranges[1], out var suffixLength))
                    return false;

                start = totalLength - suffixLength;
                end = totalLength - 1;
            }
            else if (string.IsNullOrEmpty(ranges[1]))
            {
                // Формат: bytes=500- (с 500 байта до конца)
                if (!long.TryParse(ranges[0], out start))
                    return false;

                end = totalLength - 1;
            }
            else
            {
                // Формат: bytes=0-1023 (конкретный диапазон)
                if (!long.TryParse(ranges[0], out start) || !long.TryParse(ranges[1], out end))
                    return false;
            }

            // Валидация
            if (start < 0 || end >= totalLength || start > end)
                return false;

            range = new RangeHelper
            {
                Start = start,
                End = end,
                Length = end - start + 1,
                TotalLength = totalLength
            };

            return true;
        }
    }

    private async Task CopyStreamRangeAsync(Stream source, Stream destination, long start, long length)
    {
        var buffer = new byte[4096]; // 4KB chunks
        long bytesCopied = 0;

        source.Seek(start, SeekOrigin.Begin);

        while (bytesCopied < length)
        {
            var bytesToRead = (int)Math.Min(buffer.Length, length - bytesCopied);
            var bytesRead = await source.ReadAsync(buffer, 0, bytesToRead);

            if (bytesRead == 0) break;

            await destination.WriteAsync(buffer, 0, bytesRead);
            bytesCopied += bytesRead;
        }
    }

    #endregion

        #endregion


    }
}
