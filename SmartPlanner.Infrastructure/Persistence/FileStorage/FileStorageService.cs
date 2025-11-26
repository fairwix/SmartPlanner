using System.Text.Json;

namespace SmartPlanner.Infrastructure.FileStorage;

    public class FileStorageService : IFileStorageService
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly JsonSerializerOptions _jsonOptions;

        public FileStorageService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<List<T>> ReadFromFileAsync<T>(string filePath, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!File.Exists(filePath))
                    return new List<T>();

                var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task WriteToFileAsync<T>(string filePath, List<T> entities, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var tempPath = filePath + ".tmp";
                var json = JsonSerializer.Serialize(entities, _jsonOptions);
                await File.WriteAllTextAsync(tempPath, json, cancellationToken);
                File.Move(tempPath, filePath, true);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
