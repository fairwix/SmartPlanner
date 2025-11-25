using System.Text.Json;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Infrastructure.FileStorage
{
    public class FileStorageRepository<T> where T : BaseEntity
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private List<T> _entities = new();

        public FileStorageRepository(string filePath)
        {
            _filePath = filePath;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            await EnsureLoadedAsync();
            return _entities.AsReadOnly();
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            await EnsureLoadedAsync();
            return _entities.FirstOrDefault(e => e.Id == id);
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Func<T, bool> predicate)
        {
            await EnsureLoadedAsync();
            return _entities.Where(predicate).ToList();
        }

        public virtual async Task<T> CreateAsync(T entity)
        {
            await _semaphore.WaitAsync();
            try
            {
                await EnsureLoadedAsync();
                
                entity.Id = Guid.NewGuid();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                
                _entities.Add(entity);
                await SaveChangesAsync();
                
                return entity;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            await _semaphore.WaitAsync();
            try
            {
                await EnsureLoadedAsync();
                
                var existing = _entities.FirstOrDefault(e => e.Id == entity.Id);
                if (existing == null)
                    throw new KeyNotFoundException($"Entity with id {entity.Id} not found");

                entity.UpdatedAt = DateTime.UtcNow;
                _entities.Remove(existing);
                _entities.Add(entity);
                
                await SaveChangesAsync();
                return entity;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await _semaphore.WaitAsync();
            try
            {
                await EnsureLoadedAsync();
                
                var entity = _entities.FirstOrDefault(e => e.Id == id);
                if (entity != null)
                {
                    _entities.Remove(entity);
                    await SaveChangesAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected virtual async Task SaveChangesAsync()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Атомарная запись: пишем во временный файл, затем переименовываем
            var tempPath = _filePath + ".tmp";
            var json = JsonSerializer.Serialize(_entities, options);
            await File.WriteAllTextAsync(tempPath, json);
            
            if (File.Exists(_filePath))
                File.Delete(_filePath);
                
            File.Move(tempPath, _filePath);
        }

        protected virtual async Task EnsureLoadedAsync()
        {
            if (_entities.Any()) return;

            await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists(_filePath))
                {
                    _entities = new List<T>();
                    return;
                }

                var json = await File.ReadAllTextAsync(_filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _entities = new List<T>();
                    return;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                _entities = JsonSerializer.Deserialize<List<T>>(json, options) ?? new List<T>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading data from {_filePath}", ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}