using System.Text.Json;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Infrastructure.Persistence.FileStorage;

public class FileStorageRepository<T> where T : BaseEntity, new()
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private List<T> _entities = new();

        public FileStorageRepository(string filePath)
        {
            _filePath = filePath;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken);
            return _entities.AsReadOnly();
        }

        public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken);
            return _entities.FirstOrDefault(e => e.Id == id);
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Func<T, bool> predicate, CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken);
            return _entities.Where(predicate).ToList();
        }

        public virtual async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await EnsureLoadedAsync(cancellationToken);

                // Создаем новую сущность через инициализатор, так как CreatedAt - init-only свойство
                var newEntity = new T
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    // Копируем остальные свойства с помощью рефлексии
                };

                // Копируем все остальные свойства с помощью рефлексии
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && p.CanWrite && !new[] { "Id", "CreatedAt", "UpdatedAt" }.Contains(p.Name));

                foreach (var prop in properties)
                {
                    prop.SetValue(newEntity, prop.GetValue(entity));
                }

                _entities.Add(newEntity);
                await SaveChangesAsync();

                return newEntity;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await EnsureLoadedAsync(cancellationToken);

                var existing = _entities.FirstOrDefault(e => e.Id == entity.Id);
                if (existing == null)
                    throw new KeyNotFoundException($"Entity with id {entity.Id} not found");

                // Обновляем только UpdatedAt, остальные свойства уже в entity
                var updatedEntity = existing;
                updatedEntity.UpdatedAt = DateTime.UtcNow;

                // Копируем все изменяемые свойства из entity в updatedEntity
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && p.CanWrite && !new[] { "Id", "CreatedAt", "UpdatedAt" }.Contains(p.Name));

                foreach (var prop in properties)
                {
                    prop.SetValue(updatedEntity, prop.GetValue(entity));
                }

                _entities.Remove(existing);
                _entities.Add(updatedEntity);

                await SaveChangesAsync();
                return updatedEntity;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await EnsureLoadedAsync(cancellationToken);

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

        protected virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Атомарная запись: пишем во временный файл, затем переименовываем
            var tempPath = _filePath + ".tmp";
            var json = JsonSerializer.Serialize(_entities, options);
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);

            if (File.Exists(_filePath))
                File.Delete(_filePath);

            File.Move(tempPath, _filePath);
        }

        protected virtual async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
        {
            if (_entities.Any()) return;

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!File.Exists(_filePath))
                {
                    _entities = new List<T>();
                    return;
                }

                var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
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
