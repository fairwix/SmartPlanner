using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Common.Interfaces;
using SmartPlanner.Application.Dtos.Files;
using SmartPlanner.Application.Interfaces.Services;
using SmartPlanner.Application.Services;
using SmartPlanner.Domain.Entities;
using Xunit;

namespace SmartPlanner.Application.Tests.Services
{
    public class FileServiceTests : IDisposable
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<FileService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IImageService> _mockImageService;
        private readonly FileService _service;
        private readonly string _testUploadsDirectory;
        private readonly string _testTempDirectory;

        public FileServiceTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockLogger = new Mock<ILogger<FileService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockImageService = new Mock<IImageService>();

            // Настраиваем конфигурацию для тестов
            _mockConfiguration.Setup(c => c["FileUpload:MaxSize"]).Returns("52428800"); // 50MB
            _mockConfiguration.Setup(c => c["FileUpload:Path"]).Returns("uploads");

            // Создаем временные директории для тестов
            _testUploadsDirectory = Path.Combine(Path.GetTempPath(), $"TestUploads_{Guid.NewGuid()}");
            _testTempDirectory = Path.Combine(_testUploadsDirectory, "temp");

            // Mock для CurrentDirectory
            var mockDirectory = new Mock<IDirectory>();
            _service = new FileService(
                _mockContext.Object,
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockImageService.Object);

            // Используем рефлексию для подмены путей
            SetPrivateField(_service, "_uploadsPath", _testUploadsDirectory);
            SetPrivateField(_service, "_tempPath", _testTempDirectory);

            Directory.CreateDirectory(_testUploadsDirectory);
            Directory.CreateDirectory(_testTempDirectory);
        }

        public void Dispose()
        {
            // Очищаем временные директории
            if (Directory.Exists(_testUploadsDirectory))
            {
                Directory.Delete(_testUploadsDirectory, true);
            }
        }

        #region Вспомогательные методы

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private Mock<IFormFile> CreateMockFormFile(string fileName, long length, string contentType)
        {
            var mockFile = new Mock<IFormFile>();
            var content = "Test file content";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(length);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream stream, CancellationToken token) => ms.CopyToAsync(stream, token));

            return mockFile;
        }

        private byte[] CreateTestJpeg()
        {
            using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(100, 100);
            using var ms = new MemoryStream();
            image.Save(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
            return ms.ToArray();
        }

        private byte[] CreateTestPng()
        {
            using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(100, 100);
            using var ms = new MemoryStream();
            image.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            return ms.ToArray();
        }

        #endregion

        #region Тесты валидации

        [Theory]
        [InlineData("test.jpg", true)]
        [InlineData("test.jpeg", true)]
        [InlineData("test.png", true)]
        [InlineData("test.gif", true)]
        [InlineData("test.webp", true)]
        [InlineData("test.pdf", true)]
        [InlineData("test.doc", true)]
        [InlineData("test.docx", true)]
        [InlineData("test.xls", true)]
        [InlineData("test.xlsx", true)]
        [InlineData("test.txt", true)]
        [InlineData("test.exe", false)]
        [InlineData("test.dll", false)]
        [InlineData("test.bat", false)]
        public void IsValidExtension_ReturnsCorrectResult(string fileName, bool expected)
        {
            // Arrange
            var method = typeof(FileService).GetMethod("IsValidExtension",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var result = (bool)method.Invoke(_service, new object[] { fileName });

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("image/jpeg", true)]
        [InlineData("image/png", true)]
        [InlineData("image/gif", true)]
        [InlineData("image/webp", true)]
        [InlineData("application/pdf", true)]
        [InlineData("application/msword", true)]
        [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", true)]
        [InlineData("application/vnd.ms-excel", true)]
        [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", true)]
        [InlineData("text/plain", true)]
        [InlineData("application/octet-stream", false)]
        [InlineData("application/x-msdownload", false)]
        public void IsValidMimeType_ReturnsCorrectResult(string contentType, bool expected)
        {
            // Arrange
            var method = typeof(FileService).GetMethod("IsValidMimeType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var result = (bool)method.Invoke(_service, new object[] { contentType });

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SanitizeFileName_RemovesInvalidCharacters()
        {
            // Arrange
            var method = typeof(FileService).GetMethod("SanitizeFileName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var invalidFileName = "test<file>.jpg";

            // Act
            var result = (string)method.Invoke(_service, new object[] { invalidFileName });

            // Assert
            Assert.Equal("testfile.jpg", result);
        }

        [Fact]
        public void SanitizeFileName_TruncatesLongNames()
        {
            // Arrange
            var method = typeof(FileService).GetMethod("SanitizeFileName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var longFileName = new string('a', 300) + ".jpg";

            // Act
            var result = (string)method.Invoke(_service, new object[] { longFileName });

            // Assert
            Assert.Equal(255, result.Length);
        }

        #endregion

        #region Тесты UploadFileAsync

        [Fact]
        public async Task UploadFileAsync_ValidFile_UploadsSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockFile = CreateMockFormFile("test.jpg", 1024, "image/jpeg");

            var mockDbSet = new Mock<DbSet<FileMetadata>>();
            var files = new List<FileMetadata>().AsQueryable();
            SetupMockDbSet(mockDbSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockDbSet.Object);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.UploadFileAsync(mockFile.Object, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test.jpg", result.OriginalFileName);
            Assert.Equal(1024, result.Size);
            Assert.Equal("image/jpeg", result.ContentType);
            Assert.Equal(userId, result.UploadedById);

            mockDbSet.Verify(s => s.AddAsync(It.IsAny<FileMetadata>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadFileAsync_EmptyFile_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);
            mockFile.Setup(f => f.FileName).Returns("test.jpg");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadFileAsync(mockFile.Object, userId));

            Assert.Contains("пустой", exception.Message);
        }

        [Fact]
        public async Task UploadFileAsync_FileTooLarge_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockFile = CreateMockFormFile("test.jpg", 100 * 1024 * 1024, "image/jpeg");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadFileAsync(mockFile.Object, userId));

            Assert.Contains("превышает", exception.Message);
        }

        [Fact]
        public async Task UploadFileAsync_InvalidExtension_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockFile = CreateMockFormFile("test.exe", 1024, "application/octet-stream");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadFileAsync(mockFile.Object, userId));

            Assert.Contains("расширение", exception.Message);
        }

        [Fact]
        public async Task UploadFileAsync_DuplicateFile_ReturnsExistingFile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingFileId = Guid.NewGuid();
            var fileContent = "Test file content";
            var hash = ComputeHash(fileContent);

            var mockFile = CreateMockFormFile("test.jpg", fileContent.Length, "image/jpeg");

            var existingFile = new FileMetadata
            {
                Id = existingFileId,
                FileName = "existing.jpg",
                OriginalFileName = "test.jpg",
                Hash = hash,
                UploadedById = userId
            };

            var files = new List<FileMetadata> { existingFile }.AsQueryable();
            var mockDbSet = new Mock<DbSet<FileMetadata>>();
            SetupMockDbSet(mockDbSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockDbSet.Object);

            // Act
            var result = await _service.UploadFileAsync(mockFile.Object, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingFileId, result.Id);
            Assert.Equal("test.jpg", result.OriginalFileName);

            mockDbSet.Verify(s => s.AddAsync(It.IsAny<FileMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UploadFileAsync_ImageFile_ProcessesImage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockFile = CreateMockFormFile("test.jpg", 1024, "image/jpeg");

            _mockImageService.Setup(i => i.IsImageFile("test.jpg")).Returns(true);
            _mockImageService.Setup(i => i.GetImageDimensionsAsync(It.IsAny<string>()))
                .ReturnsAsync((100, 100));
            _mockImageService.Setup(i => i.OptimizeImageAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            //_mockImageService.Setup(i => i.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ThumbnailSize>(), It.IsAny<bool>()))
                //.ReturnsAsync("thumbnail_path.jpg");

            var mockDbSet = new Mock<DbSet<FileMetadata>>();
            var files = new List<FileMetadata>().AsQueryable();
            SetupMockDbSet(mockDbSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockDbSet.Object);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.UploadFileAsync(mockFile.Object, userId);

            // Assert
            Assert.NotNull(result);
            _mockImageService.Verify(i => i.IsImageFile("test.jpg"), Times.Once);
            _mockImageService.Verify(i => i.GetImageDimensionsAsync(It.IsAny<string>()), Times.Once);
            _mockImageService.Verify(i => i.OptimizeImageAsync(It.IsAny<string>(), 85), Times.Once);
        }

        #endregion

        #region Тесты UploadMultipleFilesAsync

        [Fact]
        public async Task UploadMultipleFilesAsync_ValidFiles_UploadsAll()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockFiles = new List<IFormFile>
            {
                CreateMockFormFile("test1.jpg", 1024, "image/jpeg").Object,
                CreateMockFormFile("test2.png", 2048, "image/png").Object
            };

            var mockDbSet = new Mock<DbSet<FileMetadata>>();
            var files = new List<FileMetadata>().AsQueryable();
            SetupMockDbSet(mockDbSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockDbSet.Object);
            _mockContext.SetupSequence(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1)
                .ReturnsAsync(1);

            // Act
            var results = await _service.UploadMultipleFilesAsync(mockFiles, userId);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.Equal("test1.jpg", results[0].OriginalFileName);
            Assert.Equal("test2.png", results[1].OriginalFileName);
        }

        [Fact]
        public async Task UploadMultipleFilesAsync_TooManyFiles_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockFiles = new List<IFormFile>();
            for (int i = 0; i < 11; i++)
            {
                mockFiles.Add(CreateMockFormFile($"test{i}.jpg", 1024, "image/jpeg").Object);
            }

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadMultipleFilesAsync(mockFiles, userId));

            Assert.Contains("Максимум 10 файлов", exception.Message);
        }

        [Fact]
        public async Task UploadMultipleFilesAsync_TotalSizeExceeded_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockFiles = new List<IFormFile>
            {
                CreateMockFormFile("test1.jpg", 60 * 1024 * 1024, "image/jpeg").Object,
                CreateMockFormFile("test2.jpg", 50 * 1024 * 1024, "image/jpeg").Object
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadMultipleFilesAsync(mockFiles, userId));

            Assert.Contains("Общий размер", exception.Message);
        }

        #endregion

        #region Тесты CheckDuplicateAsync

        [Fact]
        public async Task CheckDuplicateAsync_FileExists_ReturnsDuplicateInfo()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fileHash = "testhash123";
            var existingFileId = Guid.NewGuid();

            var existingFile = new FileMetadata
            {
                Id = existingFileId,
                OriginalFileName = "existing.jpg",
                Hash = fileHash,
                Size = 1024,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UploadedById = userId
            };

            var files = new List<FileMetadata> { existingFile }.AsQueryable();
            var mockDbSet = new Mock<DbSet<FileMetadata>>();
            SetupMockDbSet(mockDbSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockDbSet.Object);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var request = new CheckDuplicateRequestDto
            {
                FileHash = fileHash,
                FileName = "test.jpg",
                FileSize = 1024
            };

            // Act
            var result = await _service.CheckDuplicateAsync(request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsDuplicate);
            Assert.Equal(existingFileId, result.ExistingFileId);
            Assert.Equal("existing.jpg", result.FileName);
            Assert.Equal(1024, result.FileSize);

            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CheckDuplicateAsync_FileNotExists_ReturnsNotDuplicate()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var files = new List<FileMetadata>().AsQueryable();
            var mockDbSet = new Mock<DbSet<FileMetadata>>();
            SetupMockDbSet(mockDbSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockDbSet.Object);

            var request = new CheckDuplicateRequestDto
            {
                FileHash = "nonexistenthash",
                FileName = "test.jpg",
                FileSize = 1024
            };

            // Act
            var result = await _service.CheckDuplicateAsync(request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsDuplicate);
            Assert.Null(result.ExistingFileId);
        }

        [Fact]
        public async Task CheckDuplicateAsync_EmptyHash_ReturnsNotDuplicate()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CheckDuplicateRequestDto
            {
                FileHash = "",
                FileName = "test.jpg",
                FileSize = 1024
            };

            // Act
            var result = await _service.CheckDuplicateAsync(request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsDuplicate);
        }

        #endregion

        #region Тесты StartChunkedUploadAsync

        [Fact]
        public async Task StartChunkedUploadAsync_ValidRequest_StartsUpload()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new ChunkedUploadStartDto
            {
                FileName = "test.jpg",
                FileSize = 1024,
                TotalChunks = 5,
                FileHash = "testhash123",
                IsPublic = true,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            var mockUploadProgressSet = new Mock<DbSet<UploadProgress>>();
            var uploads = new List<UploadProgress>().AsQueryable();
            SetupMockDbSet(mockUploadProgressSet, uploads);

            var mockFileSet = new Mock<DbSet<FileMetadata>>();
            var files = new List<FileMetadata>().AsQueryable();
            SetupMockDbSet(mockFileSet, files);

            _mockContext.Setup(c => c.UploadProgresses).Returns(mockUploadProgressSet.Object);
            _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.StartChunkedUploadAsync(request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("upload_", result.UploadId);
            Assert.Equal(0, result.Progress);
            Assert.Equal(0, result.ChunksReceived);
            Assert.Equal(5, result.TotalChunks);
            Assert.Equal("uploading", result.Status);
            Assert.False(result.IsDuplicate);

            mockUploadProgressSet.Verify(s => s.AddAsync(It.IsAny<UploadProgress>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StartChunkedUploadAsync_DuplicateFile_ReturnsDuplicateInfo()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingFileId = Guid.NewGuid();
            var fileHash = "testhash123";

            var request = new ChunkedUploadStartDto
            {
                FileName = "test.jpg",
                FileSize = 1024,
                TotalChunks = 5,
                FileHash = fileHash
            };

            var existingFile = new FileMetadata
            {
                Id = existingFileId,
                Hash = fileHash,
                UploadedById = userId
            };

            var files = new List<FileMetadata> { existingFile }.AsQueryable();
            var mockFileSet = new Mock<DbSet<FileMetadata>>();
            SetupMockDbSet(mockFileSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

            // Act
            var result = await _service.StartChunkedUploadAsync(request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("duplicate_", result.UploadId);
            Assert.Equal(100, result.Progress);
            Assert.Equal(5, result.ChunksReceived);
            Assert.Equal(5, result.TotalChunks);
            Assert.Equal("completed", result.Status);
            Assert.True(result.IsDuplicate);
            Assert.Equal(existingFileId, result.ExistingFileId);
            Assert.Contains("уже существует", result.Message);
        }

        #endregion

        #region Тесты UploadChunkAsync

        [Fact]
        public async Task UploadChunkAsync_ValidChunk_UploadsSuccessfully()
        {
            // Arrange
            var uploadId = $"upload_{Guid.NewGuid():N}";
            var userId = Guid.NewGuid();
            var mockChunk = CreateMockFormFile("chunk", 1024, "application/octet-stream");

            // Создаем запись о прогрессе в БД
            var uploadProgress = new UploadProgress
            {
                Id = Guid.NewGuid(),
                UploadId = uploadId,
                UserId = userId,
                FileName = "test.jpg",
                TotalChunks = 5,
                UploadedChunks = 0,
                Status = "uploading"
            };

            var uploads = new List<UploadProgress> { uploadProgress }.AsQueryable();
            var mockUploadSet = new Mock<DbSet<UploadProgress>>();
            SetupMockDbSet(mockUploadSet, uploads);

            _mockContext.Setup(c => c.UploadProgresses).Returns(mockUploadSet.Object);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.UploadChunkAsync(
                mockChunk.Object, uploadId, 0, 5, "test.jpg", userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(uploadId, result.UploadId);
            Assert.Equal(0.2, result.Progress, 1); // 1/5 = 0.2
            Assert.Equal(1, result.ChunksReceived);
            Assert.Equal(5, result.TotalChunks);
            Assert.Equal("uploading", result.Status);

            // Проверяем, что файл чанка создан
            var chunkPath = Path.Combine(_testTempDirectory, uploadId, "chunks", "chunk_0.part");
            Assert.True(File.Exists(chunkPath));

            // Проверяем, что метаданные созданы
            var metadataPath = Path.Combine(_testTempDirectory, uploadId, "metadata.json");
            Assert.True(File.Exists(metadataPath));
        }

        [Fact]
        public async Task UploadChunkAsync_EmptyChunk_ThrowsArgumentException()
        {
            // Arrange
            var uploadId = "test_upload";
            var userId = Guid.NewGuid();
            var mockChunk = new Mock<IFormFile>();
            mockChunk.Setup(f => f.Length).Returns(0);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadChunkAsync(mockChunk.Object, uploadId, 0, 5, "test.jpg", userId));

            Assert.Contains("не может быть пустым", exception.Message);
        }

        [Fact]
        public async Task UploadChunkAsync_ChunkTooLarge_ThrowsArgumentException()
        {
            // Arrange
            var uploadId = "test_upload";
            var userId = Guid.NewGuid();
            var mockChunk = CreateMockFormFile("chunk", 60 * 1024 * 1024, "application/octet-stream");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadChunkAsync(mockChunk.Object, uploadId, 0, 5, "test.jpg", userId));

            Assert.Contains("превышает", exception.Message);
        }

        [Fact]
        public async Task UploadChunkAsync_InvalidChunkIndex_ThrowsArgumentException()
        {
            // Arrange
            var uploadId = "test_upload";
            var userId = Guid.NewGuid();
            var mockChunk = CreateMockFormFile("chunk", 1024, "application/octet-stream");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadChunkAsync(mockChunk.Object, uploadId, 5, 5, "test.jpg", userId));

            Assert.Contains("индекс", exception.Message);
        }

        #endregion

        #region Тесты GetUploadProgressAsync

        [Fact]
        public async Task GetUploadProgressAsync_ExistsInDatabase_ReturnsProgress()
        {
            // Arrange
            var uploadId = "test_upload";
            var userId = Guid.NewGuid();

            var uploadProgress = new UploadProgress
            {
                Id = Guid.NewGuid(),
                UploadId = uploadId,
                UserId = userId,
                FileName = "test.jpg",
                TotalChunks = 10,
                UploadedChunks = 5,
                Status = "uploading"
            };

            var uploads = new List<UploadProgress> { uploadProgress }.AsQueryable();
            var mockUploadSet = new Mock<DbSet<UploadProgress>>();
            SetupMockDbSet(mockUploadSet, uploads);

            _mockContext.Setup(c => c.UploadProgresses).Returns(mockUploadSet.Object);

            // Act
            var result = await _service.GetUploadProgressAsync(uploadId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(uploadId, result.UploadId);
            Assert.Equal(0.5, result.Progress); // 5/10 = 0.5
            Assert.Equal(5, result.ChunksReceived);
            Assert.Equal(10, result.TotalChunks);
            Assert.Equal("uploading", result.Status);
        }

        [Fact]
        public async Task GetUploadProgressAsync_ExistsInFiles_ReturnsProgress()
        {
            // Arrange
            var uploadId = "test_upload";
            var userId = Guid.NewGuid();

            // Создаем временные файлы для загрузки
            var uploadPath = Path.Combine(_testTempDirectory, uploadId);
            Directory.CreateDirectory(uploadPath);
            Directory.CreateDirectory(Path.Combine(uploadPath, "chunks"));

            // Создаем несколько чанков
            for (int i = 0; i < 3; i++)
            {
                var chunkPath = Path.Combine(uploadPath, "chunks", $"chunk_{i}.part");
                await File.WriteAllTextAsync(chunkPath, $"chunk {i}");
            }

            // Создаем метаданные
            var metadata = new
            {
                UserId = userId,
                FileName = "test.jpg",
                TotalChunks = 5,
                UploadedChunks = new List<int> { 0, 1, 2 }
            };

            var metadataPath = Path.Combine(uploadPath, "metadata.json");
            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata));

            // Настраиваем пустую БД
            var uploads = new List<UploadProgress>().AsQueryable();
            var mockUploadSet = new Mock<DbSet<UploadProgress>>();
            SetupMockDbSet(mockUploadSet, uploads);
            _mockContext.Setup(c => c.UploadProgresses).Returns(mockUploadSet.Object);

            // Act
            var result = await _service.GetUploadProgressAsync(uploadId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(uploadId, result.UploadId);
            Assert.Equal(0.6, result.Progress, 1); // 3/5 = 0.6
            Assert.Equal(3, result.ChunksReceived);
            Assert.Equal(5, result.TotalChunks);
            Assert.Equal("uploading", result.Status);
        }

        [Fact]
        public async Task GetUploadProgressAsync_NotFound_ReturnsNull()
        {
            // Arrange
            var uploadId = "nonexistent";
            var userId = Guid.NewGuid();

            var uploads = new List<UploadProgress>().AsQueryable();
            var mockUploadSet = new Mock<DbSet<UploadProgress>>();
            SetupMockDbSet(mockUploadSet, uploads);
            _mockContext.Setup(c => c.UploadProgresses).Returns(mockUploadSet.Object);

            // Act
            var result = await _service.GetUploadProgressAsync(uploadId, userId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Тесты GetFileMetadataAsync

        [Fact]
        public async Task GetFileMetadataAsync_PublicFile_ReturnsMetadata()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var fileMetadata = new FileMetadata
            {
                Id = fileId,
                OriginalFileName = "test.jpg",
                IsPublic = true,
                UploadedById = Guid.NewGuid() // Другой пользователь
            };

            var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
            var mockFileSet = new Mock<DbSet<FileMetadata>>();
            SetupMockDbSet(mockFileSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

            // Act
            var result = await _service.GetFileMetadataAsync(fileId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fileId, result.Id);
            Assert.Equal("test.jpg", result.OriginalFileName);
        }

        [Fact]
        public async Task GetFileMetadataAsync_PrivateFileOwner_ReturnsMetadata()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var fileMetadata = new FileMetadata
            {
                Id = fileId,
                OriginalFileName = "private.jpg",
                IsPublic = false,
                UploadedById = userId // Владелец файла
            };

            var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
            var mockFileSet = new Mock<DbSet<FileMetadata>>();
            SetupMockDbSet(mockFileSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

            // Act
            var result = await _service.GetFileMetadataAsync(fileId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fileId, result.Id);
        }

        [Fact]
        public async Task GetFileMetadataAsync_PrivateFileNonOwner_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            var fileMetadata = new FileMetadata
            {
                Id = fileId,
                OriginalFileName = "private.jpg",
                IsPublic = false,
                UploadedById = ownerId
            };

            var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
            var mockFileSet = new Mock<DbSet<FileMetadata>>();
            SetupMockDbSet(mockFileSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);
            _mockContext.Setup(c => c.UserRoles).Returns(Mock.Of<DbSet<UserRole>>());

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetFileMetadataAsync(fileId, otherUserId));
        }

        [Fact]
        public async Task GetFileMetadataAsync_FileNotFound_ReturnsNull()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var files = new List<FileMetadata>().AsQueryable();
            var mockFileSet = new Mock<DbSet<FileMetadata>>();
            SetupMockDbSet(mockFileSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

            // Act
            var result = await _service.GetFileMetadataAsync(fileId, userId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Тесты DeleteFileAsync

        [Fact]
        public async Task DeleteFileAsync_OwnerDeletes_Success()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var fileMetadata = new FileMetadata
            {
                Id = fileId,
                OriginalFileName = "test.jpg",
                Path = "test.jpg",
                UploadedById = userId
            };

            // Создаем тестовый файл
            var filePath = Path.Combine(_testUploadsDirectory, fileMetadata.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await File.WriteAllTextAsync(filePath, "test content");

            var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
            var mockFileSet = new Mock<DbSet<FileMetadata>>();
            SetupMockDbSet(mockFileSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _service.DeleteFileAsync(fileId, userId);

            // Assert
            Assert.False(File.Exists(filePath));
            mockFileSet.Verify(s => s.Remove(It.Is<FileMetadata>(f => f.Id == fileId)), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteFileAsync_NonOwner_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            var fileMetadata = new FileMetadata
            {
                Id = fileId,
                UploadedById = ownerId
            };

            var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
            var mockFileSet = new Mock<DbSet<FileMetadata>>();
            SetupMockDbSet(mockFileSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);
            _mockContext.Setup(c => c.UserRoles).Returns(Mock.Of<DbSet<UserRole>>());

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.DeleteFileAsync(fileId, otherUserId));
        }

        [Fact]
        public async Task DeleteFileAsync_FileNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var files = new List<FileMetadata>().AsQueryable();
            var mockFileSet = new Mock<DbSet<FileMetadata>>();
            SetupMockDbSet(mockFileSet, files);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                _service.DeleteFileAsync(fileId, userId));
        }

        #endregion

        #region Тесты GetUserFilesAsync

        [Fact]
        public async Task GetUserFilesAsync_ValidParameters_ReturnsPagedResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var parameters = new FileQueryParameters
            {
                Page = 1,
                PageSize = 10,
                Search = "test",
                ContentType = "image",
                SortBy = "filename",
                SortDescending = false
            };

            var userFiles = new List<FileMetadata>
            {
                new FileMetadata
                {
                    Id = Guid.NewGuid(),
                    OriginalFileName = "test1.jpg",
                    ContentType = "image/jpeg",
                    Size = 1024,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UploadedById = userId
                },
                new FileMetadata
                {
                    Id = Guid.NewGuid(),
                    OriginalFileName = "test2.png",
                    ContentType = "image/png",
                    Size = 2048,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UploadedById = userId
                }
            }.AsQueryable();

            var mockDbSet = new Mock<DbSet<FileMetadata>>();
            SetupMockDbSet(mockDbSet, userFiles);

            _mockContext.Setup(c => c.FileMetadata).Returns(mockDbSet.Object);

            // Act
            var result = await _service.GetUserFilesAsync(userId, parameters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(10, result.PageSize);
            Assert.Equal("test1.jpg", result.Items[0].OriginalFileName);
            Assert.Equal("test2.png", result.Items[1].OriginalFileName);
        }

        #endregion

        #region Вспомогательные методы

        private string ComputeHash(string content)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(content);
            var hashBytes = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private void SetupMockDbSet<T>(Mock<DbSet<T>> mockSet, IQueryable<T> data) where T : class
        {
            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

            mockSet.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(data.Provider));

            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        }

        // Вспомогательный интерфейс для мокирования Directory
        public interface IDirectory
        {
            string GetCurrentDirectory();
            void CreateDirectory(string path);
        }

        #endregion

        #region Тесты CompleteChunkedUploadAsync

[Fact]
public async Task CompleteChunkedUploadAsync_ValidUpload_CompletesSuccessfully()
{
    // Arrange
    var uploadId = $"upload_{Guid.NewGuid():N}";
    var userId = Guid.NewGuid();

    // Создаем временные файлы для загрузки
    var uploadPath = Path.Combine(_testTempDirectory, uploadId);
    Directory.CreateDirectory(uploadPath);
    Directory.CreateDirectory(Path.Combine(uploadPath, "chunks"));

    // Создаем несколько тестовых чанков
    for (int i = 0; i < 3; i++)
    {
        var chunkPath = Path.Combine(uploadPath, "chunks", $"chunk_{i}.part");
        await File.WriteAllBytesAsync(chunkPath, CreateTestJpeg());
    }

    // Создаем метаданные
    var metadata = new
    {
        UserId = userId,
        FileName = "test.jpg",
        TotalChunks = 3,
        UploadedChunks = new List<int> { 0, 1, 2 },
        IsPublic = false,
        ExpiresAt = (DateTime?)null,
        StartedAt = DateTime.UtcNow
    };

    var metadataPath = Path.Combine(uploadPath, "metadata.json");
    await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata));

    // Настраиваем БД
    var uploadProgress = new UploadProgress
    {
        Id = Guid.NewGuid(),
        UploadId = uploadId,
        UserId = userId,
        FileName = "test.jpg",
        TotalChunks = 3,
        UploadedChunks = 3,
        Status = "assembling"
    };

    var uploads = new List<UploadProgress> { uploadProgress }.AsQueryable();
    var mockUploadSet = new Mock<DbSet<UploadProgress>>();
    SetupMockDbSet(mockUploadSet, uploads);

    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    var files = new List<FileMetadata>().AsQueryable();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.UploadProgresses).Returns(mockUploadSet.Object);
    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);
    _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    _mockImageService.Setup(i => i.IsImageFile("test.jpg")).Returns(true);
    _mockImageService.Setup(i => i.GetImageDimensionsAsync(It.IsAny<string>()))
        .ReturnsAsync((100, 100));
    _mockImageService.Setup(i => i.OptimizeImageAsync(It.IsAny<string>(), It.IsAny<int>()))
        .Returns(Task.CompletedTask);
   // _mockImageService.Setup(i => i.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ThumbnailSize>(), It.IsAny<bool>()))
    //    .ReturnsAsync("thumbnail.jpg");

    // Act
    var result = await _service.CompleteChunkedUploadAsync(uploadId, userId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("test.jpg", result.OriginalFileName);

    // Проверяем, что временные файлы удалены
    Assert.False(Directory.Exists(uploadPath));
}

[Fact]
public async Task CompleteChunkedUploadAsync_UploadNotFound_ThrowsFileNotFoundException()
{
    // Arrange
    var uploadId = "nonexistent";
    var userId = Guid.NewGuid();

    var uploads = new List<UploadProgress>().AsQueryable();
    var mockUploadSet = new Mock<DbSet<UploadProgress>>();
    SetupMockDbSet(mockUploadSet, uploads);
    _mockContext.Setup(c => c.UploadProgresses).Returns(mockUploadSet.Object);

    // Act & Assert
    await Assert.ThrowsAsync<FileNotFoundException>(() =>
        _service.CompleteChunkedUploadAsync(uploadId, userId));
}

[Fact]
public async Task CompleteChunkedUploadAsync_WrongUser_ThrowsUnauthorizedAccessException()
{
    // Arrange
    var uploadId = "test_upload";
    var ownerId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();

    var uploadProgress = new UploadProgress
    {
        Id = Guid.NewGuid(),
        UploadId = uploadId,
        UserId = ownerId,
        Status = "assembling"
    };

    var uploads = new List<UploadProgress> { uploadProgress }.AsQueryable();
    var mockUploadSet = new Mock<DbSet<UploadProgress>>();
    SetupMockDbSet(mockUploadSet, uploads);
    _mockContext.Setup(c => c.UploadProgresses).Returns(mockUploadSet.Object);

    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
        _service.CompleteChunkedUploadAsync(uploadId, otherUserId));
}

[Fact]
public async Task CompleteChunkedUploadAsync_AlreadyCompleted_ReturnsExistingFile()
{
    // Arrange
    var uploadId = "test_upload";
    var userId = Guid.NewGuid();
    var existingFileId = Guid.NewGuid();

    var uploadProgress = new UploadProgress
    {
        Id = Guid.NewGuid(),
        UploadId = uploadId,
        UserId = userId,
        Status = "completed"
    };

    var existingFile = new FileMetadata
    {
        Id = existingFileId,
        OriginalFileName = "existing.jpg",
        UploadedById = userId,
        CreatedAt = DateTime.UtcNow
    };

    var uploads = new List<UploadProgress> { uploadProgress }.AsQueryable();
    var mockUploadSet = new Mock<DbSet<UploadProgress>>();
    SetupMockDbSet(mockUploadSet, uploads);

    var files = new List<FileMetadata> { existingFile }.AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.UploadProgresses).Returns(mockUploadSet.Object);
    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

    // Act
    var result = await _service.CompleteChunkedUploadAsync(uploadId, userId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(existingFileId, result.Id);
}

[Fact]
public async Task CompleteChunkedUploadAsync_DuplicateFoundDuringAssembly_ReturnsDuplicate()
{
    // Arrange
    var uploadId = $"upload_{Guid.NewGuid():N}";
    var userId = Guid.NewGuid();
    var existingFileId = Guid.NewGuid();
    var fileHash = "testhash123";

    // Создаем временные файлы
    var uploadPath = Path.Combine(_testTempDirectory, uploadId);
    Directory.CreateDirectory(uploadPath);
    Directory.CreateDirectory(Path.Combine(uploadPath, "chunks"));

    for (int i = 0; i < 3; i++)
    {
        var chunkPath = Path.Combine(uploadPath, "chunks", $"chunk_{i}.part");
        await File.WriteAllBytesAsync(chunkPath, CreateTestJpeg());
    }

    var metadata = new
    {
        UserId = userId,
        FileName = "test.jpg",
        TotalChunks = 3,
        UploadedChunks = new List<int> { 0, 1, 2 }
    };

    var metadataPath = Path.Combine(uploadPath, "metadata.json");
    await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata));

    // Настраиваем БД с дубликатом
    var uploadProgress = new UploadProgress
    {
        Id = Guid.NewGuid(),
        UploadId = uploadId,
        UserId = userId,
        FileName = "test.jpg",
        TotalChunks = 3,
        UploadedChunks = 3,
        Status = "assembling",
        FileHash = fileHash
    };

    var existingFile = new FileMetadata
    {
        Id = existingFileId,
        Hash = fileHash,
        OriginalFileName = "existing.jpg",
        UploadedById = userId
    };

    var uploads = new List<UploadProgress> { uploadProgress }.AsQueryable();
    var mockUploadSet = new Mock<DbSet<UploadProgress>>();
    SetupMockDbSet(mockUploadSet, uploads);

    var files = new List<FileMetadata> { existingFile }.AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.UploadProgresses).Returns(mockUploadSet.Object);
    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);
    _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    // Act
    var result = await _service.CompleteChunkedUploadAsync(uploadId, userId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(existingFileId, result.Id);
    Assert.True(Directory.Exists(uploadPath) == false); // Временные файлы должны быть очищены
}

[Fact]
public async Task CompleteChunkedUploadAsync_AssembledFileTooLarge_ThrowsArgumentException()
{
    // Arrange
    var uploadId = $"upload_{Guid.NewGuid():N}";
    var userId = Guid.NewGuid();

    // Создаем большой файл
    var uploadPath = Path.Combine(_testTempDirectory, uploadId);
    Directory.CreateDirectory(uploadPath);

    var assembledPath = Path.Combine(uploadPath, "assembled.tmp");
    // Создаем файл больше максимального размера
    using (var fs = new FileStream(assembledPath, FileMode.Create))
    {
        fs.SetLength(60 * 1024 * 1024); // 60MB > 50MB
    }

    Directory.CreateDirectory(Path.Combine(uploadPath, "chunks"));

    var metadata = new
    {
        UserId = userId,
        FileName = "test.jpg",
        TotalChunks = 1,
        UploadedChunks = new List<int> { 0 }
    };

    var metadataPath = Path.Combine(uploadPath, "metadata.json");
    await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata));

    var uploadProgress = new UploadProgress
    {
        Id = Guid.NewGuid(),
        UploadId = uploadId,
        UserId = userId,
        Status = "assembling"
    };

    var uploads = new List<UploadProgress> { uploadProgress }.AsQueryable();
    var mockUploadSet = new Mock<DbSet<UploadProgress>>();
    SetupMockDbSet(mockUploadSet, uploads);
    _mockContext.Setup(c => c.UploadProgresses).Returns(mockUploadSet.Object);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
        _service.CompleteChunkedUploadAsync(uploadId, userId));

    Assert.Contains("превышает", exception.Message);
}

#endregion

#region Тесты DownloadFileAsync

[Fact]
public async Task DownloadFileAsync_ValidFile_ReturnsStream()
{
    // Arrange
    var fileId = Guid.NewGuid();
    var userId = Guid.NewGuid();

    // Создаем тестовый файл
    var relativePath = Path.Combine("users", userId.ToString(), "test.jpg");
    var fullPath = Path.Combine(_testUploadsDirectory, relativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
    await File.WriteAllTextAsync(fullPath, "Test file content");

    var fileMetadata = new FileMetadata
    {
        Id = fileId,
        OriginalFileName = "test.jpg",
        Path = relativePath,
        ContentType = "image/jpeg",
        IsPublic = true,
        ExpiresAt = DateTime.UtcNow.AddDays(1),
        UploadedById = userId
    };

    var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);
    _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    // Act
    var result = await _service.DownloadFileAsync(fileId, userId);

    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.Stream);
    Assert.Equal("image/jpeg", result.ContentType);
    Assert.Equal("test.jpg", result.FileName);

    // Проверяем, что счетчик скачиваний увеличился
    _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task DownloadFileAsync_FileExpired_ThrowsInvalidOperationException()
{
    // Arrange
    var fileId = Guid.NewGuid();
    var userId = Guid.NewGuid();

    var fileMetadata = new FileMetadata
    {
        Id = fileId,
        OriginalFileName = "test.jpg",
        Path = "test.jpg",
        ContentType = "image/jpeg",
        ExpiresAt = DateTime.UtcNow.AddDays(-1), // Просрочен
        UploadedById = userId
    };

    var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        _service.DownloadFileAsync(fileId, userId));

    Assert.Contains("истёк", exception.Message);
}

[Fact]
public async Task DownloadFileAsync_FileNotFoundOnDisk_ThrowsFileNotFoundException()
{
    // Arrange
    var fileId = Guid.NewGuid();
    var userId = Guid.NewGuid();

    var fileMetadata = new FileMetadata
    {
        Id = fileId,
        OriginalFileName = "test.jpg",
        Path = "nonexistent.jpg", // Файла нет на диске
        ContentType = "image/jpeg",
        UploadedById = userId
    };

    var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

    // Act & Assert
    await Assert.ThrowsAsync<FileNotFoundException>(() =>
        _service.DownloadFileAsync(fileId, userId));
}

#endregion

#region Тесты GetFileStreamAsync

[Fact]
public async Task GetFileStreamAsync_ValidFile_ReturnsFileStream()
{
    // Arrange
    var fileId = Guid.NewGuid();
    var userId = Guid.NewGuid();

    // Создаем тестовый файл
    var relativePath = Path.Combine("users", userId.ToString(), "test.jpg");
    var fullPath = Path.Combine(_testUploadsDirectory, relativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
    await File.WriteAllTextAsync(fullPath, "Test file content");

    var fileMetadata = new FileMetadata
    {
        Id = fileId,
        OriginalFileName = "test.jpg",
        Path = relativePath,
        ContentType = "image/jpeg",
        Size = 18, // Длина "Test file content"
        IsPublic = true,
        ExpiresAt = DateTime.UtcNow.AddDays(1),
        UploadedById = userId
    };

    var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

    // Act
    var result = await _service.GetFileStreamAsync(fileId, userId);

    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.Stream);
    Assert.Equal("image/jpeg", result.ContentType);
    Assert.Equal("test.jpg", result.FileName);
    Assert.Equal(18, result.FileSize);

    // Проверяем, что поток можно прочитать
    using var reader = new StreamReader(result.Stream);
    var content = await reader.ReadToEndAsync();
    Assert.Equal("Test file content", content);
}

[Fact]
public async Task GetFileStreamAsync_PrivateFileNonOwner_ThrowsUnauthorizedAccessException()
{
    // Arrange
    var fileId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();

    var fileMetadata = new FileMetadata
    {
        Id = fileId,
        OriginalFileName = "private.jpg",
        Path = "private.jpg",
        ContentType = "image/jpeg",
        IsPublic = false,
        UploadedById = ownerId
    };

    var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);
    _mockContext.Setup(c => c.UserRoles).Returns(Mock.Of<DbSet<UserRole>>());

    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
        _service.GetFileStreamAsync(fileId, otherUserId));
}

#endregion

#region Тесты FileExistsAsync и GetFileOwnerAsync

[Fact]
public async Task FileExistsAsync_FileExists_ReturnsTrue()
{
    // Arrange
    var fileId = Guid.NewGuid();

    var fileMetadata = new FileMetadata
    {
        Id = fileId,
        OriginalFileName = "test.jpg"
    };

    var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

    // Act
    var result = await _service.FileExistsAsync(fileId);

    // Assert
    Assert.True(result);
}

[Fact]
public async Task FileExistsAsync_FileNotExists_ReturnsFalse()
{
    // Arrange
    var fileId = Guid.NewGuid();

    var files = new List<FileMetadata>().AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

    // Act
    var result = await _service.FileExistsAsync(fileId);

    // Assert
    Assert.False(result);
}

[Fact]
public async Task GetFileOwnerAsync_FileExists_ReturnsOwnerId()
{
    // Arrange
    var fileId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();

    var fileMetadata = new FileMetadata
    {
        Id = fileId,
        OriginalFileName = "test.jpg",
        UploadedById = ownerId
    };

    var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

    // Act
    var result = await _service.GetFileOwnerAsync(fileId);

    // Assert
    Assert.Equal(ownerId, result);
}

[Fact]
public async Task GetFileOwnerAsync_FileNotExists_ReturnsEmptyGuid()
{
    // Arrange
    var fileId = Guid.NewGuid();

    var files = new List<FileMetadata>().AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

    // Act
    var result = await _service.GetFileOwnerAsync(fileId);

    // Assert
    Assert.Equal(Guid.Empty, result);
}

#endregion

#region Тесты приватных методов валидации сигнатур

[Fact]
public void ValidateFileSignature_JpegFile_ReturnsTrue()
{
    // Arrange
    var method = typeof(FileService).GetMethod("ValidateFileSignatureFromStream",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
    using var stream = new MemoryStream(jpegBytes);

    // Act
    var result = (bool)method.Invoke(_service, new object[] { stream, ".jpg" });

    // Assert
    Assert.True(result);
}

[Fact]
public void ValidateFileSignature_PngFile_ReturnsTrue()
{
    // Arrange
    var method = typeof(FileService).GetMethod("ValidateFileSignatureFromStream",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    using var stream = new MemoryStream(pngBytes);

    // Act
    var result = (bool)method.Invoke(_service, new object[] { stream, ".png" });

    // Assert
    Assert.True(result);
}

[Fact]
public void ValidateFileSignature_WrongSignature_ReturnsFalse()
{
    // Arrange
    var method = typeof(FileService).GetMethod("ValidateFileSignatureFromStream",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    var wrongBytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    using var stream = new MemoryStream(wrongBytes);

    // Act
    var result = (bool)method.Invoke(_service, new object[] { stream, ".jpg" });

    // Assert
    Assert.False(result);
}

[Fact]
public void ValidateFileSignature_UnknownExtension_ReturnsTrue()
{
    // Arrange
    var method = typeof(FileService).GetMethod("ValidateFileSignatureFromStream",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    var bytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };
    using var stream = new MemoryStream(bytes);

    // Act
    var result = (bool)method.Invoke(_service, new object[] { stream, ".unknown" });

    // Assert
    Assert.True(result); // Для неизвестных расширений проверка пропускается
}

[Fact]
public void ValidateFileSignature_ShortStream_ReturnsFalse()
{
    // Arrange
    var method = typeof(FileService).GetMethod("ValidateFileSignatureFromStream",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    var shortBytes = new byte[] { 0xFF }; // Слишком коротко для JPEG
    using var stream = new MemoryStream(shortBytes);

    // Act
    var result = (bool)method.Invoke(_service, new object[] { stream, ".jpg" });

    // Assert
    Assert.False(result);
}

#endregion

#region Тесты GetContentType и MapToDto

[Fact]
public void GetContentType_KnownExtensions_ReturnsCorrectMimeType()
{
    // Arrange
    var method = typeof(FileService).GetMethod("GetContentType",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    // Act & Assert
    Assert.Equal("image/jpeg", method.Invoke(_service, new object[] { ".jpg" }));
    Assert.Equal("image/jpeg", method.Invoke(_service, new object[] { ".jpeg" }));
    Assert.Equal("image/png", method.Invoke(_service, new object[] { ".png" }));
    Assert.Equal("image/gif", method.Invoke(_service, new object[] { ".gif" }));
    Assert.Equal("application/pdf", method.Invoke(_service, new object[] { ".pdf" }));
    Assert.Equal("application/msword", method.Invoke(_service, new object[] { ".doc" }));
    Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        method.Invoke(_service, new object[] { ".docx" }));
    Assert.Equal("application/octet-stream", method.Invoke(_service, new object[] { ".unknown" }));
}

[Fact]
public void MapToDto_MapsAllProperties()
{
    // Arrange
    var method = typeof(FileService).GetMethod("MapToDto",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

    var file = new FileMetadata
    {
        Id = Guid.NewGuid(),
        FileName = "test.jpg",
        OriginalFileName = "original.jpg",
        ContentType = "image/jpeg",
        Size = 1024,
        Path = "path/to/file.jpg",
        Hash = "testhash123",
        IsPublic = true,
        ExpiresAt = DateTime.UtcNow.AddDays(1),
        DownloadCount = 5,
        UploadedById = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow.AddDays(-1),
        UpdatedAt = DateTime.UtcNow,
        Width = 800,
        Height = 600,
        CameraModel = "Test Camera",
        Location = "Test Location",
        ThumbnailPath = "path/to/thumbnail.jpg",
        MediumPath = "path/to/medium.jpg"
    };

    // Act
    var result = (FileMetadataDto)method.Invoke(null, new object[] { file });

    // Assert
    Assert.NotNull(result);
    Assert.Equal(file.Id, result.Id);
    Assert.Equal(file.FileName, result.FileName);
    Assert.Equal(file.OriginalFileName, result.OriginalFileName);
    Assert.Equal(file.ContentType, result.ContentType);
    Assert.Equal(file.Size, result.Size);
    Assert.Equal(file.Path, result.Path);
    Assert.Equal(file.Hash, result.Hash);
    Assert.Equal(file.IsPublic, result.IsPublic);
    Assert.Equal(file.ExpiresAt, result.ExpiresAt);
    Assert.Equal(file.DownloadCount, result.DownloadCount);
    Assert.Equal(file.UploadedById, result.UploadedById);
    Assert.Equal(file.CreatedAt, result.CreatedAt);
    Assert.Equal(file.UpdatedAt, result.UpdatedAt);
}


[Fact]
public async Task UploadFileAsync_CancellationTokenCancelled_ThrowsTaskCanceledException()
{
    // Arrange
    var userId = Guid.NewGuid();
    var mockFile = CreateMockFormFile("test.jpg", 1024, "image/jpeg");
    var cancellationToken = new CancellationToken(canceled: true);

    // Act & Assert
    await Assert.ThrowsAsync<TaskCanceledException>(() =>
        _service.UploadFileAsync(mockFile.Object, userId, cancellationToken: cancellationToken));
}

[Fact]
public async Task UploadFileAsync_ExceptionDuringProcessing_ThrowsAndLogsError()
{
    // Arrange
    var userId = Guid.NewGuid();
    var mockFile = CreateMockFormFile("test.jpg", 1024, "image/jpeg");

    var mockDbSet = new Mock<DbSet<FileMetadata>>();
    var files = new List<FileMetadata>().AsQueryable();
    SetupMockDbSet(mockDbSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockDbSet.Object);
    _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Database error"));

    // Act & Assert
    var exception = await Assert.ThrowsAsync<Exception>(() =>
        _service.UploadFileAsync(mockFile.Object, userId));

    Assert.Contains("Database error", exception.Message);

    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ошибка при загрузке файла")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
        Times.Once);
}

[Fact]
public async Task UploadFileAsync_ImageProcessingFails_ContinuesWithoutError()
{
    // Arrange
    var userId = Guid.NewGuid();
    var mockFile = CreateMockFormFile("test.jpg", 1024, "image/jpeg");

    _mockImageService.Setup(i => i.IsImageFile("test.jpg")).Returns(true);
    _mockImageService.Setup(i => i.GetImageDimensionsAsync(It.IsAny<string>()))
        .ThrowsAsync(new Exception("Image processing failed"));

    var mockDbSet = new Mock<DbSet<FileMetadata>>();
    var files = new List<FileMetadata>().AsQueryable();
    SetupMockDbSet(mockDbSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockDbSet.Object);
    _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    // Act
    var result = await _service.UploadFileAsync(mockFile.Object, userId);

    // Assert
    Assert.NotNull(result);
    // Обработка изображения не должна прерывать загрузку файла
    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ошибка при обработке изображения")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
        Times.Once);
}

[Fact]
public async Task GetUserFilesAsync_EmptyResult_ReturnsEmptyPagedResult()
{
    // Arrange
    var userId = Guid.NewGuid();
    var parameters = new FileQueryParameters
    {
        Page = 1,
        PageSize = 10
    };

    var files = new List<FileMetadata>().AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

    // Act
    var result = await _service.GetUserFilesAsync(userId, parameters);

    // Assert
    Assert.NotNull(result);
    Assert.Empty(result.Items);
    Assert.Equal(0, result.TotalCount);
   // Assert.Equal(1, result.Page);
    Assert.Equal(10, result.PageSize);
}

[Fact]
public async Task GetUserFilesAsync_DifferentSortOptions_SortsCorrectly()
{
    // Arrange
    var userId = Guid.NewGuid();

    var files = new List<FileMetadata>
    {
        new FileMetadata
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "b.jpg",
            Size = 200,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UploadedById = userId
        },
        new FileMetadata
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "a.jpg",
            Size = 100,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UploadedById = userId
        },
        new FileMetadata
        {
            Id = Guid.NewGuid(),
            OriginalFileName = "c.jpg",
            Size = 300,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UploadedById = userId
        }
    }.AsQueryable();

    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);

    // Test sort by filename ascending
    var params1 = new FileQueryParameters
    {
        Page = 1,
        PageSize = 10,
        SortBy = "filename",
        SortDescending = false
    };

    var result1 = await _service.GetUserFilesAsync(userId, params1);
    Assert.Equal("a.jpg", result1.Items[0].OriginalFileName);
    Assert.Equal("b.jpg", result1.Items[1].OriginalFileName);
    Assert.Equal("c.jpg", result1.Items[2].OriginalFileName);

    // Test sort by filename descending
    var params2 = new FileQueryParameters
    {
        Page = 1,
        PageSize = 10,
        SortBy = "filename",
        SortDescending = true
    };

    var result2 = await _service.GetUserFilesAsync(userId, params2);
    Assert.Equal("c.jpg", result2.Items[0].OriginalFileName);
    Assert.Equal("b.jpg", result2.Items[1].OriginalFileName);
    Assert.Equal("a.jpg", result2.Items[2].OriginalFileName);

    // Test sort by size ascending (default)
    var params3 = new FileQueryParameters
    {
        Page = 1,
        PageSize = 10,
        SortBy = "size"
    };

    var result3 = await _service.GetUserFilesAsync(userId, params3);
    Assert.Equal(100, result3.Items[0].Size);
    Assert.Equal(200, result3.Items[1].Size);
    Assert.Equal(300, result3.Items[2].Size);

    // Test default sort (by date)
    var params4 = new FileQueryParameters
    {
        Page = 1,
        PageSize = 10
    };

    var result4 = await _service.GetUserFilesAsync(userId, params4);
    // Должны быть отсортированы по дате создания (последние первые)
    Assert.Equal(DateTime.UtcNow.AddDays(-1).Date, result4.Items[0].CreatedAt.Date);
    Assert.Equal(DateTime.UtcNow.AddDays(-2).Date, result4.Items[1].CreatedAt.Date);
}

[Fact]
public async Task DeleteFileAsync_AdminCanDeleteAnyFile()
{
    // Arrange
    var fileId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();
    var adminId = Guid.NewGuid();

    var fileMetadata = new FileMetadata
    {
        Id = fileId,
        OriginalFileName = "test.jpg",
        Path = "test.jpg",
        UploadedById = ownerId
    };

    // Создаем тестовый файл
    var filePath = Path.Combine(_testUploadsDirectory, fileMetadata.Path);
    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
    await File.WriteAllTextAsync(filePath, "test content");

    var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    var userRoles = new List<UserRole>
    {
        new UserRole
        {
            UserId = adminId,
            Role = new Role { Name = "Admin" }
        }
    }.AsQueryable();

    var mockRoleSet = new Mock<DbSet<UserRole>>();
    SetupMockDbSet(mockRoleSet, userRoles);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);
    _mockContext.Setup(c => c.UserRoles).Returns(mockRoleSet.Object);
    _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    // Act
    await _service.DeleteFileAsync(fileId, adminId);

    // Assert
    Assert.False(File.Exists(filePath));
    mockFileSet.Verify(s => s.Remove(It.Is<FileMetadata>(f => f.Id == fileId)), Times.Once);
}

[Fact]
public async Task GetFileMetadataAsync_AdminCanAccessPrivateFile()
{
    // Arrange
    var fileId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();
    var adminId = Guid.NewGuid();

    var fileMetadata = new FileMetadata
    {
        Id = fileId,
        OriginalFileName = "private.jpg",
        IsPublic = false,
        UploadedById = ownerId
    };

    var files = new List<FileMetadata> { fileMetadata }.AsQueryable();
    var mockFileSet = new Mock<DbSet<FileMetadata>>();
    SetupMockDbSet(mockFileSet, files);

    var userRoles = new List<UserRole>
    {
        new UserRole
        {
            UserId = adminId,
            Role = new Role { Name = "Admin" }
        }
    }.AsQueryable();

    var mockRoleSet = new Mock<DbSet<UserRole>>();
    SetupMockDbSet(mockRoleSet, userRoles);

    _mockContext.Setup(c => c.FileMetadata).Returns(mockFileSet.Object);
    _mockContext.Setup(c => c.UserRoles).Returns(mockRoleSet.Object);

    // Act
    var result = await _service.GetFileMetadataAsync(fileId, adminId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(fileId, result.Id);

    #endregion

}


    }


    // Вспомогательные классы для тестирования async операций
    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
        public T Current => _inner.Current;
    }

    internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;
        public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;
        public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<T>(expression);
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);
        public object Execute(Expression expression) => _inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var result = Execute(expression);
            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(typeof(TResult).GetGenericArguments()[0])
                .Invoke(null, new[] { result })!;
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }
}
