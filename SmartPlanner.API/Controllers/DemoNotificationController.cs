using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPlanner.API.Services;
using System.Security.Claims;

namespace SmartPlanner.API.Controllers
{
    [ApiController]
    [Route("api/demo")]
    [Authorize]
    public class DemoNotificationController : ControllerBase
    {
        private readonly IFileNotificationService _notificationService;
        private readonly ILogger<DemoNotificationController> _logger;

        public DemoNotificationController(
            IFileNotificationService notificationService,
            ILogger<DemoNotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Тест уведомления о загрузке файла
        /// </summary>
        [HttpPost("test-file-upload")]
        public async Task<IActionResult> TestFileUploadNotification(
            [FromBody] TestFileNotificationRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? "demo-user";

            _logger.LogInformation("🧪 Тест уведомления о загрузке файла от {UserId}", userId);

            await _notificationService.NotifyFileUploadedAsync(
                userId,
                request.FileName,
                request.FileSize,
                request.FileId,
                request.IsDuplicate);

            return Ok(new
            {
                success = true,
                message = "Тестовое уведомление отправлено",
                userId = userId,
                fileName = request.FileName,
                fileSize = request.FileSize,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Тест уведомления об удалении файла
        /// </summary>
        [HttpPost("test-file-delete")]
        public async Task<IActionResult> TestFileDeleteNotification(
            [FromBody] TestFileDeleteRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? "demo-user";

            _logger.LogInformation("🧪 Тест уведомления об удалении файла от {UserId}", userId);

            await _notificationService.NotifyFileDeletedAsync(
                userId,
                request.FileName,
                request.FileId,
                request.Reason);

            return Ok(new
            {
                success = true,
                message = "Тестовое уведомление об удалении отправлено",
                userId = userId,
                fileName = request.FileName,
                fileId = request.FileId,
                reason = request.Reason,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Тест прогресса загрузки
        /// </summary>
        [HttpPost("test-upload-progress")]
        public async Task<IActionResult> TestUploadProgress(
            [FromBody] TestProgressRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? "demo-user";

            for (int i = 0; i <= 100; i += 10)
            {
                await _notificationService.NotifyUploadProgressAsync(
                    userId, request.UploadId, i);
                await Task.Delay(500); // Имитация задержки
            }

            return Ok(new
            {
                success = true,
                message = "Тест прогресса завершен",
                uploadId = request.UploadId,
                userId = userId
            });
        }
    }

    public class TestFileNotificationRequest
    {
        public string FileName { get; set; } = "test-file.pdf";
        public long FileSize { get; set; } = 1024 * 1024; // 1MB
        public Guid FileId { get; set; } = Guid.NewGuid();
        public bool IsDuplicate { get; set; } = false;
    }

    public class TestFileDeleteRequest
    {
        public string FileName { get; set; } = "test-file.pdf";
        public Guid FileId { get; set; } = Guid.NewGuid();
        public string Reason { get; set; } = "Тестовое удаление";
    }

    public class TestProgressRequest
    {
        public string UploadId { get; set; } = "test-upload-123";
    }
}
