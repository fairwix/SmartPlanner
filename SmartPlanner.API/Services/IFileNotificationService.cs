using System;

namespace SmartPlanner.API.Services
{
    public interface IFileNotificationService
    {
        Task NotifyFileUploadedAsync(string userId, string fileName, long fileSize,
            Guid fileId, bool isDuplicate = false);
        Task NotifyFileDeletedAsync(string userId, string fileName, Guid fileId, string reason = "");
        Task NotifyUploadProgressAsync(string userId, string uploadId, int progress);
        Task NotifyUploadStartedAsync(string userId, string uploadId, string fileName, long fileSize);
        Task NotifyUploadCompletedAsync(string userId, string uploadId, string fileName, bool success);
        Task NotifyFileDownloadedAsync(string userId, string fileName, Guid fileId);
    }
}
