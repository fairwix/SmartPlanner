namespace SmartPlanner.Infrastructure.FileStorage
{
    public interface IFileStorageService
    {
        Task<List<T>> ReadFromFileAsync<T>(string filePath, CancellationToken cancellationToken = default);
        Task WriteToFileAsync<T>(string filePath, List<T> entities, CancellationToken cancellationToken = default);
    }
}