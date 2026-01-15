using Microsoft.AspNetCore.Http;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Application.Dtos.Files;

namespace SmartPlanner.Application.Interfaces.Services
{
    public interface IFileService
    {
        Task<FileMetadataDto> UploadFileAsync(
            IFormFile file,
            Guid userId,
            bool isPublic = false,
            DateTime? expiresAt = null,
            CancellationToken cancellationToken = default);

        Task<FileMetadataDto> UploadLargeFileAsync(
            IFormFile file,
            Guid userId,
            bool isPublic = false,
            DateTime? expiresAt = null,
            CancellationToken cancellationToken = default);

        Task<List<FileMetadataDto>> UploadMultipleFilesAsync(
            List<IFormFile> files,
            Guid userId,
            bool isPublic = false,
            DateTime? expiresAt = null,
            CancellationToken cancellationToken = default);


        Task<(Stream Stream, string ContentType, string FileName)> DownloadFileAsync(
            Guid fileId,
            Guid? userId = null,
            CancellationToken cancellationToken = default);

        Task<FileMetadataDto> GetFileMetadataAsync(
            Guid fileId,
            Guid? userId = null,
            CancellationToken cancellationToken = default);

        Task DeleteFileAsync(
            Guid fileId,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<PagedResult<FileMetadataDto>> GetUserFilesAsync(
            Guid userId,
            FileQueryParameters parameters,
            CancellationToken cancellationToken = default);

        Task<bool> FileExistsAsync(Guid fileId, CancellationToken cancellationToken = default);

        Task<Guid> GetFileOwnerAsync(Guid fileId, CancellationToken cancellationToken = default);

        Task<(FileStream Stream, string ContentType, string FileName, long FileSize)>
            GetFileStreamAsync(Guid fileId, Guid? userId = null, CancellationToken cancellationToken = default);


        Task<ChunkedUploadProgressDto> UploadChunkAsync(
            IFormFile chunk,
            string uploadId,
            int chunkIndex,
            int totalChunks,
            string fileName,
            Guid userId,
            bool isPublic = false,
            DateTime? expiresAt = null,
            CancellationToken cancellationToken = default);

        Task<FileMetadataDto> CompleteChunkedUploadAsync(
            string uploadId,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<ChunkedUploadProgressResponseDto> StartChunkedUploadAsync(
            ChunkedUploadStartDto request,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<ChunkedUploadProgressDto?> GetUploadProgressAsync(
            string uploadId,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<CheckDuplicateResponseDto> CheckDuplicateAsync(
            CheckDuplicateRequestDto request,
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Потоковая загрузка файла (для больших файлов)
        /// </summary>
        Task<StreamUploadResultDto> UploadFileStreamAsync(
            Stream fileStream,
            StreamUploadDto uploadDto,
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
