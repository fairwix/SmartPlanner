using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Dtos.Files;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.Services
{
    public class FileService : IFileService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<FileService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _uploadsPath;
        private readonly string _tempPath;
        private readonly IImageService _imageService;

        // Настройки валидации
        private readonly long _maxFileSize = 50 * 1024 * 1024; // 50MB
        private readonly long _maxTotalSize = 100 * 1024 * 1024; // 100MB
        private readonly Dictionary<string, List<byte[]>> _fileSignatures = new()
        {
            { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
            { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
            { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
            { ".doc", new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } } },
            { ".docx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".xls", new List<byte[]> { new byte[] { 0xD0, 0xCF, 0x11, 0xE0 } } },
            { ".xlsx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }
        };

        public FileService(
            IApplicationDbContext context,
            ILogger<FileService> logger,
            IConfiguration configuration, IImageService imageService)
        {
            _imageService = imageService;
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            _tempPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "temp");

            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        #region Вспомогательные методы валидации

        private bool IsValidExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var allowedExtensions = new[]
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp",
                ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt",
                ".bin", ".mp4", ".avi", ".mov", ".mp3", ".wav", ".zip", ".rar"
            };
            return allowedExtensions.Contains(extension);
        }

        private bool IsValidMimeType(string contentType)
        {
            var allowedMimeTypes = new[]
            {
                // Изображения
                "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp",

                // PDF и документы
                "application/pdf", "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "text/plain",

                // Видео ← ДОБАВЬ ЭТО
                "video/mp4", "video/mpeg", "video/quicktime", "video/x-msvideo",
                "video/x-ms-wmv", "video/webm", "video/ogg",

                // Аудио
                "audio/mpeg", "audio/wav", "audio/ogg", "audio/webm",

                // Архивы
                "application/zip", "application/x-rar-compressed", "application/x-tar",

                // Общие бинарные файлы
                "application/octet-stream", "binary/octet-stream"
            };
            return allowedMimeTypes.Contains(contentType.ToLowerInvariant());
        }
        public async Task IncrementDownloadCountAsync(
            Guid fileId,
            Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var file = await _context.FileMetadata
                    .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

                if (file != null)
                {
                    file.DownloadCount++;
                    file.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogDebug("Счетчик скачиваний увеличен для файла {FileId}: {Count}",
                        fileId, file.DownloadCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось увеличить счетчик скачиваний для файла {FileId}", fileId);
                // Не прерываем выполнение из-за ошибки счетчика
            }
        }

        private bool ValidateFileSignature(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!_fileSignatures.ContainsKey(extension))
                return true;

            var signatures = _fileSignatures[extension];
            var header = new byte[signatures[0].Length];
            stream.Read(header, 0, header.Length);
            stream.Position = 0;

            return signatures.Any(signature => header.Take(signature.Length).SequenceEqual(signature));
        }

        private bool ValidateFileSignatureFromStream(Stream stream, string fileExtension)
        {
            var extension = fileExtension.ToLowerInvariant();
            if (!_fileSignatures.ContainsKey(extension))
                return true;

            var signatures = _fileSignatures[extension];
            var header = new byte[signatures[0].Length];
            var read = stream.Read(header, 0, header.Length);
            if (read < header.Length)
                return false;

            stream.Position = 0;
            return signatures.Any(signature => header.Take(signature.Length).SequenceEqual(signature));
        }


        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName
                .Where(ch => !invalidChars.Contains(ch))
                .ToArray());

            if (sanitized.Length > 255)
                sanitized = sanitized.Substring(0, 255);

            return sanitized;
        }

        #endregion

        #region Основные методы

         public async Task<FileMetadataDto> UploadFileAsync(
        IFormFile file,
        Guid userId,
        bool isPublic = false,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Начало загрузки файла: {FileName} пользователем {UserId}", file.FileName, userId);

            if (file.Length == 0)
                throw new ArgumentException("Файл пустой");
            if (file.Length > _maxFileSize)
                throw new ArgumentException($"Размер файла превышает {_maxFileSize / 1024 / 1024}MB");
            if (!IsValidExtension(file.FileName))
                throw new ArgumentException("Недопустимое расширение файла");
            if (!IsValidMimeType(file.ContentType))
                throw new ArgumentException("Недопустимый MIME-тип");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            if (!ValidateFileSignatureFromStream(memoryStream, Path.GetExtension(file.FileName)))
                throw new ArgumentException("Сигнатура файла не соответствует расширению");
            memoryStream.Position = 0;

            var originalFileName = SanitizeFileName(file.FileName);
            var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
            var now = DateTime.UtcNow;
            var datePath = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"), now.Day.ToString("D2"));
            var userPath = Path.Combine("users", userId.ToString());
            var relativePath = Path.Combine(userPath, datePath, safeFileName);


            string fileHash;
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = await sha256.ComputeHashAsync(memoryStream);
                fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
            memoryStream.Position = 0;


            var existingFile = await _context.FileMetadata
                .FirstOrDefaultAsync(f => f.Hash == fileHash && f.UploadedById == userId, cancellationToken);
            if (existingFile != null)
            {
                _logger.LogInformation("Дубликат файла найден: {FileId}", existingFile.Id);
                return MapToDto(existingFile);
            }


            var fileMetadata = new FileMetadata
            {
                Id = Guid.NewGuid(),
                FileName = safeFileName,
                OriginalFileName = originalFileName,
                ContentType = file.ContentType,
                Size = file.Length,
                Path = relativePath,
                Hash = fileHash,
                IsPublic = isPublic,
                ExpiresAt = expiresAt,
                DownloadCount = 0,
                UploadedById = userId,
                CreatedAt = now,
                UpdatedAt = now,
                CameraModel = "",
                Location = "",
                ThumbnailPath = "",
                MediumPath = ""
            };

            await _context.FileMetadata.AddAsync(fileMetadata, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var fullPath = Path.Combine(_uploadsPath, relativePath);
            var directory = Path.GetDirectoryName(fullPath)!;
            Directory.CreateDirectory(directory);

            // После сохранения файла добавляем:
            if (_imageService.IsImageFile(originalFileName))
            {
                await ProcessImageAsync(fileMetadata, fullPath);
                await _context.SaveChangesAsync(cancellationToken); // Сохраняем обновленные метаданные
            }

            using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await memoryStream.CopyToAsync(fileStream, cancellationToken);
            }

            _logger.LogInformation("Файл успешно загружен: {FileId}", fileMetadata.Id);
            return MapToDto(fileMetadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке файла: {FileName}", file.FileName);
            throw;
        }
    }

         public async Task<FileMetadataDto> UploadLargeFileAsync(
    IFormFile file,
    Guid userId,
    bool isPublic = false,
    DateTime? expiresAt = null,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Начало загрузки БОЛЬШОГО файла: {FileName} пользователем {UserId}",
            file.FileName, userId);

        if (file.Length == 0)
            throw new ArgumentException("Файл пустой");

        // Увеличиваем лимит для больших файлов
        long largeFileLimit = 2_000_000_000; // 2GB
        if (file.Length > largeFileLimit)
            throw new ArgumentException($"Размер файла превышает {largeFileLimit / 1024 / 1024 / 1024}GB");

        if (!IsValidExtension(file.FileName))
            throw new ArgumentException("Недопустимое расширение файла");
        if (!IsValidMimeType(file.ContentType))
            throw new ArgumentException("Недопустимый MIME-тип");

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        if (!ValidateFileSignatureFromStream(memoryStream, Path.GetExtension(file.FileName)))
            throw new ArgumentException("Сигнатура файла не соответствует расширению");
        memoryStream.Position = 0;

        var originalFileName = SanitizeFileName(file.FileName);
        var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
        var now = DateTime.UtcNow;
        var datePath = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"), now.Day.ToString("D2"));
        var userPath = Path.Combine("users", userId.ToString());
        var relativePath = Path.Combine(userPath, datePath, safeFileName);

        string fileHash;
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = await sha256.ComputeHashAsync(memoryStream);
            fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
        memoryStream.Position = 0;

        var existingFile = await _context.FileMetadata
            .FirstOrDefaultAsync(f => f.Hash == fileHash && f.UploadedById == userId, cancellationToken);
        if (existingFile != null)
        {
            _logger.LogInformation("Дубликат файла найден: {FileId}", existingFile.Id);
            return MapToDto(existingFile);
        }

        var fileMetadata = new FileMetadata
        {
            Id = Guid.NewGuid(),
            FileName = safeFileName,
            OriginalFileName = originalFileName,
            ContentType = file.ContentType,
            Size = file.Length,
            Path = relativePath,
            Hash = fileHash,
            IsPublic = isPublic,
            ExpiresAt = expiresAt,
            DownloadCount = 0,
            UploadedById = userId,
            CreatedAt = now,
            UpdatedAt = now,
            CameraModel = "",
            Location = "",
            ThumbnailPath = "",
            MediumPath = ""
        };

        await _context.FileMetadata.AddAsync(fileMetadata, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var fullPath = Path.Combine(_uploadsPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        if (_imageService.IsImageFile(originalFileName))
        {
            await ProcessImageAsync(fileMetadata, fullPath);
            await _context.SaveChangesAsync(cancellationToken);
        }

        using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await memoryStream.CopyToAsync(fileStream, cancellationToken);
        }

        _logger.LogInformation("БОЛЬШОЙ файл успешно загружен: {FileId}", fileMetadata.Id);
        return MapToDto(fileMetadata);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при загрузке БОЛЬШОГО файла: {FileName}", file.FileName);
        throw;
    }
}

         public async Task<StreamUploadResultDto> UploadFileStreamAsync(
    Stream fileStream,
    StreamUploadDto uploadDto,
    Guid userId,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Начало потоковой загрузки файла: {FileName} пользователем {UserId}",
            uploadDto.OriginalFileName, userId);

        // Валидация DTO
        if (string.IsNullOrEmpty(uploadDto.OriginalFileName))
            throw new ArgumentException("Имя файла обязательно");

        var fileExtension = Path.GetExtension(uploadDto.OriginalFileName)?.ToLowerInvariant();
        if (!IsValidExtension(uploadDto.OriginalFileName))
            throw new ArgumentException($"Недопустимое расширение файла: {fileExtension}");

        if (!IsValidMimeType(uploadDto.ContentType))
            throw new ArgumentException($"Недопустимый MIME-тип: {uploadDto.ContentType}");

        // Создаем временный файл для обработки потока
        var tempFileName = $"{Guid.NewGuid()}{fileExtension}";
        var tempFilePath = Path.Combine(_tempPath, tempFileName);

        // Создаем директорию для временных файлов если ее нет
        Directory.CreateDirectory(_tempPath);

        long fileSize = 0;

        // 1. Пишем поток во временный файл
        using (var tempFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(tempFileStream, 81920, cancellationToken);
            await tempFileStream.FlushAsync(cancellationToken);
            fileSize = tempFileStream.Length;
        }

        // 2. Проверяем размер файла
        if (fileSize == 0)
            throw new ArgumentException("Файл пустой");

        if (fileSize > _maxFileSize)
            throw new ArgumentException($"Размер файла превышает {_maxFileSize / 1024 / 1024}MB");

        // 3. Проверяем сигнатуру файла
        using (var verifyStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
        {
            if (!ValidateFileSignatureFromStream(verifyStream, fileExtension ?? ""))
                throw new ArgumentException("Сигнатура файла не соответствует расширению");
        }

        // 4. Вычисляем хэш файла
        string fileHash;
        using (var sha256 = SHA256.Create())
        using (var stream = File.OpenRead(tempFilePath))
        {
            var hashBytes = await sha256.ComputeHashAsync(stream);
            fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        // 5. Проверяем дубликат
        var existingFile = await _context.FileMetadata
            .FirstOrDefaultAsync(f => f.Hash == fileHash && f.UploadedById == userId, cancellationToken);

        if (existingFile != null)
        {
            // Удаляем временный файл
            File.Delete(tempFilePath);

            _logger.LogInformation("Обнаружен дубликат файла: {FileId}", existingFile.Id);

            return new StreamUploadResultDto
            {
                IsDuplicate = true,
                ExistingFileId = existingFile.Id,
                FileName = existingFile.FileName,
                OriginalFileName = existingFile.OriginalFileName,
                Size = existingFile.Size,
                ContentType = existingFile.ContentType,
                IsPublic = existingFile.IsPublic,
                ExpiresAt = existingFile.ExpiresAt,
                CreatedAt = existingFile.CreatedAt,
                Message = "Файл уже существует"
            };
        }

        // 6. Генерируем окончательное имя файла и путь
        var safeFileName = $"{Guid.NewGuid()}{fileExtension}";
        var now = DateTime.UtcNow;
        var datePath = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"), now.Day.ToString("D2"));
        var userPath = Path.Combine("users", userId.ToString());
        var relativePath = Path.Combine(userPath, datePath, safeFileName);
        var fullPath = Path.Combine(_uploadsPath, relativePath);

        // Создаем директорию
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        // 7. Перемещаем временный файл в окончательное место
        File.Move(tempFilePath, fullPath);

        // 8. Создаем запись в БД
        var fileMetadata = new FileMetadata
        {
            Id = Guid.NewGuid(),
            FileName = safeFileName,
            OriginalFileName = SanitizeFileName(uploadDto.OriginalFileName),
            ContentType = uploadDto.ContentType,
            Size = fileSize,
            Path = relativePath,
            Hash = fileHash,
            IsPublic = uploadDto.IsPublic,
            ExpiresAt = uploadDto.ExpiresAt,
            DownloadCount = 0,
            UploadedById = userId,
            CreatedAt = now,
            UpdatedAt = now,
            CameraModel = "",
            Location = "",
            ThumbnailPath = "",
            MediumPath = ""
        };

        // 9. Если это изображение - обрабатываем
        if (_imageService.IsImageFile(uploadDto.OriginalFileName))
        {
            await ProcessImageAsync(fileMetadata, fullPath);
        }

        await _context.FileMetadata.AddAsync(fileMetadata, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Потоковая загрузка завершена: {FileId}", fileMetadata.Id);

        // 10. Возвращаем результат
        return new StreamUploadResultDto
        {
            Id = fileMetadata.Id,
            FileName = fileMetadata.FileName,
            OriginalFileName = fileMetadata.OriginalFileName,
            Size = fileMetadata.Size,
            ContentType = fileMetadata.ContentType,
            IsPublic = fileMetadata.IsPublic,
            ExpiresAt = fileMetadata.ExpiresAt,
            CreatedAt = fileMetadata.CreatedAt
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при потоковой загрузке файла {FileName} пользователем {UserId}",
            uploadDto.OriginalFileName, userId);
        throw;
    }
}

    public async Task<List<FileMetadataDto>> UploadMultipleFilesAsync(
        List<IFormFile> files,
        Guid userId,
        bool isPublic = false,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        if (files.Count > 10)
            throw new ArgumentException("Максимум 10 файлов за раз");

        var totalSize = files.Sum(f => f.Length);
        if (totalSize > _maxTotalSize)
            throw new ArgumentException($"Общий размер файлов превышает {_maxTotalSize / 1024 / 1024}MB");

        var results = new List<FileMetadataDto>();

        try
        {
            foreach (var file in files)
            {
                var result = await UploadFileAsync(file, userId, isPublic, expiresAt, cancellationToken);
                results.Add(result);
            }

            return results;
        }
        catch
        {
            // При ошибке — удаляем уже загруженные файлы (если нужно)
            // Но это сложно без soft-delete и временных файлов.
            // Пока просто пробрасываем исключение.
            throw;
        }
    }

        private async Task ProcessImageAsync(FileMetadata fileMetadata, string fullPath)
    {
        try
        {
            if (!_imageService.IsImageFile(fileMetadata.OriginalFileName))
                return;

            _logger.LogInformation("Обработка изображения: {FileName}", fileMetadata.OriginalFileName);

            var (width, height) = await _imageService.GetImageDimensionsAsync(fullPath);
            fileMetadata.Width = width;
            fileMetadata.Height = height;

            var exifData = await _imageService.ExtractExifDataAsync(fullPath);

            if (fileMetadata.IsPublic && exifData.Any())
            {
                await _imageService.RemoveExifDataAsync(fullPath);
                _logger.LogInformation("EXIF данные удалены для публичного файла: {FileId}", fileMetadata.Id);
            }

            await _imageService.OptimizeImageAsync(fullPath, quality: 85);

            var originalDirectory = Path.GetDirectoryName(fullPath)!;

            var smallThumbnailPath = await _imageService.GenerateThumbnailAsync(
                fullPath, originalDirectory, ThumbnailSize.Small, crop: true);

            var mediumThumbnailPath = await _imageService.GenerateThumbnailAsync(
                fullPath, originalDirectory, ThumbnailSize.Medium, crop: false);

            fileMetadata.ThumbnailPath = Path.GetRelativePath(_uploadsPath, smallThumbnailPath);
            fileMetadata.MediumPath = Path.GetRelativePath(_uploadsPath, mediumThumbnailPath);

            _logger.LogInformation("Обработка изображения завершена: {FileId}", fileMetadata.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке изображения {FileId}", fileMetadata.Id);
            // Не прерываем загрузку файла из-за ошибки обработки изображения
        }
    }

        public async Task<ChunkedUploadProgressResponseDto> StartChunkedUploadAsync(
    ChunkedUploadStartDto request,
    Guid userId,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Начало чанковой загрузки для файла: {FileName}", request.FileName);

        // Проверяем дубликат по хешу
        if (!string.IsNullOrEmpty(request.FileHash))
        {
            var duplicateCheck = await CheckDuplicateAsync(new CheckDuplicateRequestDto
            {
                FileHash = request.FileHash,
                FileName = request.FileName,
                FileSize = request.FileSize
            }, userId, cancellationToken);

            if (duplicateCheck.IsDuplicate)
            {
                _logger.LogInformation("Дубликат найден перед началом загрузки: {FileName}", request.FileName);

                return new ChunkedUploadProgressResponseDto
                {
                    UploadId = $"duplicate_{Guid.NewGuid()}",
                    Progress = 100,
                    ChunksReceived = request.TotalChunks,
                    TotalChunks = request.TotalChunks,
                    Status = "completed",
                    IsDuplicate = true,
                    ExistingFileId = duplicateCheck.ExistingFileId,
                    Message = "Файл уже существует в системе"
                };
            }
        }

        // Создаем уникальный ID для загрузки
        var uploadId = $"upload_{Guid.NewGuid():N}";

        // Создаем запись о прогрессе в БД
        var uploadProgress = new UploadProgress
        {
            Id = Guid.NewGuid(),
            UploadId = uploadId,
            UserId = userId,
            FileName = request.FileName,
            TotalChunks = request.TotalChunks,
            UploadedChunks = 0,
            FileHash = request.FileHash,
            IsPublic = request.IsPublic ?? false,
            ExpiresAt = request.ExpiresAt,
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = "uploading"
        };

        await _context.UploadProgresses.AddAsync(uploadProgress, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Чанковая загрузка начата: {UploadId}", uploadId);

        return new ChunkedUploadProgressResponseDto
        {
            UploadId = uploadId,
            Progress = 0,
            ChunksReceived = 0,
            TotalChunks = request.TotalChunks,
            Status = "uploading",
            IsDuplicate = false,
            Message = "Загрузка начата"
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при начале чанковой загрузки для файла {FileName}", request.FileName);
        throw;
    }
}

        public async Task<CheckDuplicateResponseDto> CheckDuplicateAsync(
            CheckDuplicateRequestDto request,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FileHash))
                    return new CheckDuplicateResponseDto { IsDuplicate = false };

                var existingFile = await _context.FileMetadata
                    .FirstOrDefaultAsync(f => f.Hash == request.FileHash && f.UploadedById == userId, cancellationToken);

                if (existingFile != null)
                {
                    // Обновляем счетчик скачиваний (логика "использования существующего")
                    existingFile.DownloadCount++;
                    existingFile.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Найден дубликат файла по хешу {FileHash}", request.FileHash);

                    return new CheckDuplicateResponseDto
                    {
                        IsDuplicate = true,
                        ExistingFileId = existingFile.Id,
                        FileName = existingFile.OriginalFileName,
                        FileSize = existingFile.Size,
                        UploadedAt = existingFile.CreatedAt
                    };
                }

                return new CheckDuplicateResponseDto { IsDuplicate = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке дубликата файла {FileName}", request.FileName);
                throw;
            }
        }

        public async Task<ChunkedUploadProgressDto?> GetUploadProgressAsync(
    string uploadId,
    Guid userId,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Сначала проверяем в БД
        var progress = await _context.UploadProgresses
            .FirstOrDefaultAsync(p => p.UploadId == uploadId && p.UserId == userId, cancellationToken);

        if (progress != null)
        {
            return new ChunkedUploadProgressDto
            {
                UploadId = progress.UploadId,
                Progress = progress.TotalChunks > 0
                    ? (double)progress.UploadedChunks / progress.TotalChunks
                    : 0,
                ChunksReceived = progress.UploadedChunks,
                TotalChunks = progress.TotalChunks,
                Status = progress.Status
            };
        }

        // Если нет в БД, проверяем временные файлы (совместимость со старой системой)
        var uploadPath = Path.Combine(_tempPath, uploadId);
        if (!Directory.Exists(uploadPath))
            return null;

        var metadataPath = Path.Combine(uploadPath, "metadata.json");
        if (!File.Exists(metadataPath))
            return null;

        var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
        var metadata = JsonSerializer.Deserialize<ChunkedUploadMetadata>(json);

        if (metadata == null || metadata.UserId != userId)
            return null;

        var chunksPath = Path.Combine(uploadPath, "chunks");
        var uploadedCount = Directory.Exists(chunksPath)
            ? Directory.GetFiles(chunksPath).Length
            : 0;

        return new ChunkedUploadProgressDto
        {
            UploadId = uploadId,
            Progress = (double)uploadedCount / metadata.TotalChunks,
            ChunksReceived = uploadedCount,
            TotalChunks = metadata.TotalChunks,
            Status = uploadedCount == metadata.TotalChunks ? "assembling" : "uploading"
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при получении прогресса загрузки {UploadId}", uploadId);
        return null;
    }
}


    public async Task<ChunkedUploadProgressDto> UploadChunkAsync(
    IFormFile chunk,
    string uploadId,
    int chunkIndex,
    int totalChunks,
    string fileName,
    Guid userId,
    bool isPublic = false,
    DateTime? expiresAt = null,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Загрузка chunk {ChunkIndex}/{TotalChunks} для uploadId {UploadId}",
            chunkIndex, totalChunks, uploadId);

        // ВАЛИДАЦИЯ (остается как было)
        if (chunk == null || chunk.Length == 0)
            throw new ArgumentException("Chunk не может быть пустым");

        if (chunk.Length > _maxFileSize)
            throw new ArgumentException($"Размер chunk превышает {_maxFileSize / 1024 / 1024}MB");

        if (chunkIndex < 0 || chunkIndex >= totalChunks)
            throw new ArgumentException("Некорректный индекс chunk");

        if (totalChunks <= 0 || totalChunks > 1000)
            throw new ArgumentException("Некорректное общее количество chunks");

        var uploadProgress = await _context.UploadProgresses
            .FirstOrDefaultAsync(p => p.UploadId == uploadId, cancellationToken);

        if (uploadProgress == null)
        {
            // Создаем новую запись о прогрессе если ее нет
            uploadProgress = new UploadProgress
            {
                Id = Guid.NewGuid(),
                UploadId = uploadId,
                UserId = userId,
                FileName = fileName,
                TotalChunks = totalChunks,
                UploadedChunks = 1, // Этот чанк будет первым
                IsPublic = isPublic,
                ExpiresAt = expiresAt,
                StartedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = "uploading"
            };
            await _context.UploadProgresses.AddAsync(uploadProgress, cancellationToken);
        }
        else
        {
            // Проверяем права доступа
            if (uploadProgress.UserId != userId)
                throw new UnauthorizedAccessException("Нет прав для продолжения этой загрузки");

            // Проверяем статус
            if (uploadProgress.Status == "completed")
                throw new InvalidOperationException("Загрузка уже завершена");

            if (uploadProgress.Status == "failed")
                throw new InvalidOperationException("Загрузка завершилась с ошибкой");
        }

        var uploadPath = Path.Combine(_tempPath, uploadId);
        var chunksPath = Path.Combine(uploadPath, "chunks");
        Directory.CreateDirectory(chunksPath);

        var chunkPath = Path.Combine(chunksPath, $"chunk_{chunkIndex}.part");

        // Проверяем, не загружен ли уже этот чанк
        var isNewChunk = !File.Exists(chunkPath);

        // Сохраняем chunk
        using (var stream = new FileStream(chunkPath, FileMode.Create))
        {
            await chunk.CopyToAsync(stream, cancellationToken);
        }

        // Обновляем метаданные в файле
        var metadataPath = Path.Combine(uploadPath, "metadata.json");
        ChunkedUploadMetadata metadata;

        if (File.Exists(metadataPath))
        {
            var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
            metadata = JsonSerializer.Deserialize<ChunkedUploadMetadata>(json);

            if (!metadata.UploadedChunks.Contains(chunkIndex))
            {
                metadata.UploadedChunks.Add(chunkIndex);
                metadata.LastChunkAt = DateTime.UtcNow;
            }
        }
        else
        {
            metadata = new ChunkedUploadMetadata
            {
                UserId = userId,
                FileName = fileName,
                TotalChunks = totalChunks,
                UploadedChunks = new List<int> { chunkIndex },
                IsPublic = isPublic,
                ExpiresAt = expiresAt,
                StartedAt = DateTime.UtcNow,
                LastChunkAt = DateTime.UtcNow
            };
        }

        // Сохраняем обновленные метаданные
        var jsonData = JsonSerializer.Serialize(metadata);
        await File.WriteAllTextAsync(metadataPath, jsonData, cancellationToken);

        if (isNewChunk)
        {
            uploadProgress.UploadedChunks++;
        }

        uploadProgress.UpdatedAt = DateTime.UtcNow;

        // Обновляем статус, если все чанки загружены
        var uploadedCount = metadata.UploadedChunks.Count;
        if (uploadedCount == totalChunks)
        {
            uploadProgress.Status = "assembling";
        }

        await _context.SaveChangesAsync(cancellationToken);

        var progress = (double)uploadedCount / totalChunks;

        _logger.LogInformation("Прогресс загрузки {UploadId}: {Uploaded}/{Total} ({Progress:P})",
            uploadId, uploadedCount, totalChunks, progress);

        return new ChunkedUploadProgressDto
        {
            UploadId = uploadId,
            Progress = progress,
            ChunksReceived = uploadedCount,
            TotalChunks = totalChunks,
            Status = uploadedCount == totalChunks ? "assembling" : "uploading"
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при загрузке chunk {ChunkIndex} для uploadId {UploadId}",
            chunkIndex, uploadId);

        try
        {
            var progress = await _context.UploadProgresses
                .FirstOrDefaultAsync(p => p.UploadId == uploadId, cancellationToken);

            if (progress != null)
            {
                progress.Status = "failed";
                progress.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception innerEx)
        {
            _logger.LogError(innerEx, "Ошибка при обновлении статуса загрузки {UploadId} после ошибки", uploadId);
        }

        throw;
    }
}


    public async Task<FileMetadataDto> CompleteChunkedUploadAsync(
    string uploadId,
    Guid userId,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation("Завершение chunked upload {UploadId}", uploadId);

        var uploadProgress = await _context.UploadProgresses
            .FirstOrDefaultAsync(p => p.UploadId == uploadId, cancellationToken);

        if (uploadProgress == null)
            throw new FileNotFoundException($"Загрузка {uploadId} не найдена");

        if (uploadProgress.UserId != userId)
            throw new UnauthorizedAccessException("Нет прав на завершение этой загрузки");

        // Проверяем статус - если уже завершена, возвращаем существующий файл
        if (uploadProgress.Status == "completed")
        {
            var existingFiles = await _context.FileMetadata
                .Where(f => f.OriginalFileName == uploadProgress.FileName &&
                           f.UploadedById == userId &&
                           f.CreatedAt >= uploadProgress.StartedAt)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(cancellationToken);

            if (existingFiles.Any())
            {
                return MapToDto(existingFiles.First());
            }
        }

        // Обновляем статус
        uploadProgress.Status = "assembling";
        uploadProgress.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var uploadPath = Path.Combine(_tempPath, uploadId);
        var metadataPath = Path.Combine(uploadPath, "metadata.json");

        if (!File.Exists(metadataPath))
            throw new FileNotFoundException($"Метаданные загрузки {uploadId} не найдены");

        // Читаем метаданные
        var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
        var metadata = JsonSerializer.Deserialize<ChunkedUploadMetadata>(json);

        if (metadata.UserId != userId)
            throw new UnauthorizedAccessException("Нет прав на завершение этой загрузки");

        if (metadata.UploadedChunks.Count != metadata.TotalChunks)
            throw new InvalidOperationException("Не все chunks загружены");

        var chunksPath = Path.Combine(uploadPath, "chunks");
        var chunks = Directory.GetFiles(chunksPath)
            .OrderBy(f =>
            {
                var fileName = Path.GetFileNameWithoutExtension(f);
                return int.Parse(fileName.Split('_')[1]);
            })
            .ToList();

        // Создаем временный файл для сборки
        var assembledPath = Path.Combine(uploadPath, "assembled.tmp");

        using (var outputStream = new FileStream(assembledPath, FileMode.Create, FileAccess.Write))
        {
            foreach (var chunkPath in chunks)
            {
                using var chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read);
                await chunkStream.CopyToAsync(outputStream, cancellationToken);
            }
        }

        // Валидируем собранный файл
        var fileInfo = new FileInfo(assembledPath);

        if (fileInfo.Length > _maxFileSize)
            throw new ArgumentException($"Собранный файл превышает {_maxFileSize / 1024 / 1024}MB");

        // Проверяем сигнатуру файла
        using (var stream = new FileStream(assembledPath, FileMode.Open, FileAccess.Read))
        {
            var extension = Path.GetExtension(metadata.FileName);
            if (!ValidateFileSignatureFromStream(stream, extension))
                throw new ArgumentException("Сигнатура файла не соответствует расширению");

            stream.Position = 0;

            // Вычисляем хеш
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            var fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            // === НОВЫЙ КОД: Сохраняем хеш в записи о прогрессе ===
            uploadProgress.FileHash = fileHash;
            await _context.SaveChangesAsync(cancellationToken);

            // === НОВЫЙ КОД: Проверка дубликата ===
            var existingFile = await _context.FileMetadata
                .FirstOrDefaultAsync(f => f.Hash == fileHash && f.UploadedById == userId, cancellationToken);

            if (existingFile != null)
            {
                _logger.LogInformation("Дубликат файла найден при chunked upload: {FileId}", existingFile.Id);

                // Очищаем временные файлы
                CleanupTempFiles(uploadPath);

                // Обновляем статус загрузки
                uploadProgress.Status = "completed";
                uploadProgress.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                return MapToDto(existingFile);
            }

            stream.Position = 0;

            // Используем существующую логику для сохранения файла
            var now = DateTime.UtcNow;
            var safeFileName = $"{Guid.NewGuid()}{extension}";
            var datePath = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"), now.Day.ToString("D2"));
            var userPath = Path.Combine("users", userId.ToString());
            var relativePath = Path.Combine(userPath, datePath, safeFileName);
            var fullPath = Path.Combine(_uploadsPath, relativePath);

            var directory = Path.GetDirectoryName(fullPath)!;
            Directory.CreateDirectory(directory);

            using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            // Создаем запись в БД
            var fileMetadata = new FileMetadata
            {
                Id = Guid.NewGuid(),
                FileName = safeFileName,
                OriginalFileName = SanitizeFileName(metadata.FileName),
                ContentType = GetContentType(extension),
                Size = fileInfo.Length,
                Path = relativePath,
                Hash = fileHash,
                IsPublic = metadata.IsPublic,
                ExpiresAt = metadata.ExpiresAt,
                DownloadCount = 0,
                UploadedById = userId,
                CreatedAt = now,
                UpdatedAt = now,
                CameraModel = "",
                Location = "",
                ThumbnailPath = "",
                MediumPath = ""
            };

            // После сохранения файла в постоянное хранилище
            if (_imageService.IsImageFile(metadata.FileName))
            {
                await ProcessImageAsync(fileMetadata, fullPath);
                await _context.SaveChangesAsync(cancellationToken);
            }

            await _context.FileMetadata.AddAsync(fileMetadata, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Очищаем временные файлы
            CleanupTempFiles(uploadPath);

            // === НОВЫЙ КОД: Обновляем статус загрузки ===
            uploadProgress.Status = "completed";
            uploadProgress.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Chunked upload {UploadId} завершен успешно, создан файл {FileId}",
                uploadId, fileMetadata.Id);

            return MapToDto(fileMetadata);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при завершении chunked upload {UploadId}", uploadId);

        // НОВЫЙ КОД: Обновляем статус при ошибке
        try
        {
            var progress = await _context.UploadProgresses
                .FirstOrDefaultAsync(p => p.UploadId == uploadId, cancellationToken);

            if (progress != null)
            {
                progress.Status = "failed";
                progress.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception innerEx)
        {
            _logger.LogError(innerEx, "Ошибка при обновлении статуса загрузки {UploadId} после ошибки", uploadId);
        }

        throw;
    }
}
private void CleanupTempFiles(string uploadPath)
{
    try
    {
        if (Directory.Exists(uploadPath))
        {
            Directory.Delete(uploadPath, true);
            _logger.LogDebug("Временные файлы очищены: {Path}", uploadPath);
        }
    }
    catch (Exception ex)
    {
        // Логируем, но не прерываем выполнение
        _logger.LogWarning(ex, "Ошибка при очистке временных файлов: {Path}", uploadPath);
    }
}


    // Добавить вспомогательный метод в FileService
    private string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

        public async Task<(Stream Stream, string ContentType, string FileName)> DownloadFileAsync(
            Guid fileId,
            Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            var fileMetadata = await GetFileMetadataAsync(fileId, userId, cancellationToken);

            if (fileMetadata == null)
                throw new FileNotFoundException("Файл не найден");

            // Проверка срока действия
            if (fileMetadata.ExpiresAt.HasValue && fileMetadata.ExpiresAt.Value < DateTime.UtcNow)
                throw new InvalidOperationException("Срок действия файла истёк");

            var fullPath = Path.Combine(_uploadsPath, fileMetadata.Path);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Файл не найден на диске");

            // Инкремент счетчика скачиваний
            var fileEntity = await _context.FileMetadata
                .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

            if (fileEntity != null)
            {
                fileEntity.DownloadCount++;
                fileEntity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return (stream, fileMetadata.ContentType, fileMetadata.OriginalFileName);
        }

        public async Task<(FileStream Stream, string ContentType, string FileName, long FileSize)>
            GetFileStreamAsync(Guid fileId, Guid? userId = null, CancellationToken cancellationToken = default)
        {
            var fileMetadata = await GetFileMetadataAsync(fileId, userId, cancellationToken);

            if (fileMetadata == null)
                throw new FileNotFoundException("Файл не найден");

            if (fileMetadata.ExpiresAt.HasValue && fileMetadata.ExpiresAt.Value < DateTime.UtcNow)
                throw new InvalidOperationException("Срок действия файла истёк");

            var fullPath = Path.Combine(_uploadsPath, fileMetadata.Path);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Файл не найден на диске");

            var fileStream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true
            );

            return (fileStream, fileMetadata.ContentType, fileMetadata.OriginalFileName, fileMetadata.Size);
        }

        public async Task<FileMetadataDto> GetFileMetadataAsync(
            Guid fileId,
            Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            var file = await _context.FileMetadata
                .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

            if (file == null)
                return null;

            // Проверка прав доступа
            if (!file.IsPublic && (!userId.HasValue || file.UploadedById != userId.Value))
            {
                // Проверка на админа
                var isAdmin = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == "Admin",
                        cancellationToken);

                if (!isAdmin)
                    throw new UnauthorizedAccessException("Нет доступа к файлу");
            }

            return MapToDto(file);
        }

        public async Task DeleteFileAsync(
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var file = await _context.FileMetadata
                .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

            if (file == null)
                throw new FileNotFoundException("Файл не найден");

            // Проверка прав
            if (file.UploadedById != userId)
            {
                var isAdmin = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == "Admin",
                        cancellationToken);

                if (!isAdmin)
                    throw new UnauthorizedAccessException("Нет прав на удаление файла");
            }

            // Удаление файла с диска
            var fullPath = Path.Combine(_uploadsPath, file.Path);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // Удаление thumbnails (если будут реализованы)
            // ...

            // Удаление записи из БД
            _context.FileMetadata.Remove(file);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<PagedResult<FileMetadataDto>> GetUserFilesAsync(
            Guid userId,
            FileQueryParameters parameters,
            CancellationToken cancellationToken = default)
        {
            var query = _context.FileMetadata
                .Where(f => f.UploadedById == userId)
                .AsQueryable();

            // Фильтры
            if (!string.IsNullOrEmpty(parameters.Search))
            {
                query = query.Where(f => f.OriginalFileName.Contains(parameters.Search));
            }

            if (!string.IsNullOrEmpty(parameters.ContentType))
            {
                query = query.Where(f => f.ContentType.StartsWith(parameters.ContentType));
            }

            if (parameters.IsPublic.HasValue)
            {
                query = query.Where(f => f.IsPublic == parameters.IsPublic.Value);
            }

            // Сортировка
            query = parameters.SortBy?.ToLower() switch
            {
                "size" => parameters.SortDescending
                    ? query.OrderByDescending(f => f.Size)
                    : query.OrderBy(f => f.Size),
                "filename" => parameters.SortDescending
                    ? query.OrderByDescending(f => f.OriginalFileName)
                    : query.OrderBy(f => f.OriginalFileName),
                _ => parameters.SortDescending
                    ? query.OrderByDescending(f => f.CreatedAt)
                    : query.OrderBy(f => f.CreatedAt)
            };

            // Пагинация - используем твой существующий PagedResult<T>
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(f => MapToDto(f))
                .ToListAsync(cancellationToken);

            return PagedResult<FileMetadataDto>.Create(items, totalCount, parameters.Page, parameters.PageSize);
        }

        public async Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default)
        {
            return await _context.FileMetadata
                .AnyAsync(f => f.Id == fileId, cancellationToken);
        }

        public async Task<Guid> GetFileOwnerAsync(Guid fileId, CancellationToken cancellationToken = default)
        {
            var file = await _context.FileMetadata
                .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

            return file?.UploadedById ?? Guid.Empty;
        }

        #endregion

        #region Вспомогательные методы

        private static FileMetadataDto MapToDto(FileMetadata file)
        {
            return new FileMetadataDto
            {
                Id = file.Id,
                FileName = file.FileName,
                OriginalFileName = file.OriginalFileName,
                ContentType = file.ContentType,
                Size = file.Size,
                Path = file.Path,
                Hash = file.Hash,
                IsPublic = file.IsPublic,
                ExpiresAt = file.ExpiresAt,
                DownloadCount = file.DownloadCount,
                UploadedById = file.UploadedById,
                CreatedAt = file.CreatedAt,
                UpdatedAt = file.UpdatedAt
            };
        }


        private class ChunkedUploadMetadata
        {
            public Guid UserId { get; set; }
            public string FileName { get; set; }
            public int TotalChunks { get; set; }
            public List<int> UploadedChunks { get; set; } = new();
            public bool IsPublic { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime? LastChunkAt { get; set; }
        }

        #endregion
    }
}
