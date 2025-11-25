using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SmartPlanner.Application.Common.Dtos;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Infrastructure.Repositories 
{
    public class FileStorageRepository<T> where T : BaseEntity
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private List<T> _cache;
        private readonly JsonSerializerOptions _jsonOptions;

        public FileStorageRepository(string filePath) 
        {
            _filePath = filePath;
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }
        
        protected async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken);

                if (_cache != null) 
                    return _cache;
                
                if (!File.Exists(_filePath))
                    return new List<T>();

                var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
                _cache = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
                return _cache;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var items = await GetAllAsync(cancellationToken);
            return items.FirstOrDefault(x => x.Id == id);
        }

        protected async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            var items = await GetAllAsync(cancellationToken);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            
            items.Add(entity);
            await SaveChangesAsync(items, cancellationToken);
            return entity;
        }

        protected async Task<T?> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            var items = await GetAllAsync(cancellationToken);
            var index = items.FindIndex(x => x.Id == entity.Id);
            if (index == -1) 
                return null;

            entity.UpdatedAt = DateTime.UtcNow;
            items[index] = entity;
            await SaveChangesAsync(items, cancellationToken);
            return entity;
        }

        protected async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var items = await GetAllAsync(cancellationToken);
            var entity = items.FirstOrDefault(x => x.Id == id);
            if (entity == null) 
                return false;

            items.Remove(entity);
            await SaveChangesAsync(items, cancellationToken);
            return true;
        }

        protected async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var items = await GetAllAsync(cancellationToken);
            var compiledPredicate = predicate.Compile();
            return items.Where(compiledPredicate).ToList();
        }

        protected async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_cache != null)
            {
                await SaveChangesAsync(_cache, cancellationToken);
            }
        }

        private async Task SaveChangesAsync(List<T> items, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var tempFilePath = _filePath + ".tmp";
                var json = JsonSerializer.Serialize(items, _jsonOptions);
                
                await File.WriteAllTextAsync(tempFilePath, json, cancellationToken);
                File.Move(tempFilePath, _filePath, true);
                _cache = items;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Базовый метод для пагинации любых данных
        /// </summary>
        protected async Task<PagedResult<T>> GetPagedAsync(
            List<T> allItems,
            PaginationRequest pagination,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var totalCount = allItems.Count;
            
            var pagedItems = allItems
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToList();

            return PagedResult<T>.Create(pagedItems, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        /// <summary>
        /// Базовый метод для поиска с пагинацией
        /// </summary>
        protected async Task<PagedResult<T>> SearchPagedAsync(
            List<T> allItems,
            Func<T, bool> predicate,
            PaginationRequest pagination,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var filteredItems = allItems.Where(predicate).ToList();
            return await GetPagedAsync(filteredItems, pagination, cancellationToken);
        }

        /// <summary>
        /// Универсальный метод для пагинации с сортировкой
        /// </summary>
        protected async Task<PagedResult<T>> GetPagedAsync(
            List<T> allItems,
            PaginationRequest pagination,
            Func<IEnumerable<T>, IOrderedEnumerable<T>> sorter,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var totalCount = allItems.Count;
            
            var sortedItems = sorter(allItems);
            var pagedItems = sortedItems
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToList();

            return PagedResult<T>.Create(pagedItems, totalCount, pagination.PageNumber, pagination.PageSize);
        }
    }
}