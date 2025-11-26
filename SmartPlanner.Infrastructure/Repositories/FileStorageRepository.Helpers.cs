using System;
using System.Reflection;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Infrastructure.Repositories;

public static class FileStorageRepositoryHelpers
{
    // Делаем методы публичными для доступа из других файлов

    public static T CopyEntityProperties<T>(T source, T destination)
    {
        if (source == null || destination == null) return destination;

        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.Name != "Id" &&
                       p.Name != "CreatedAt" && p.Name != "UpdatedAt");

        foreach (var property in properties)
        {
            var value = property.GetValue(source);
            property.SetValue(destination, value);
        }

        return destination;
    }

    public static T SetEntityMetadata<T>(T entity, Guid id, DateTime createdAt, DateTime updatedAt)
    {
        if (entity == null) return entity;

        var type = typeof(T);
        var idProperty = type.GetProperty("Id");
        var createdAtProperty = type.GetProperty("CreatedAt");
        var updatedAtProperty = type.GetProperty("UpdatedAt");

        if (idProperty != null && idProperty.CanWrite) idProperty.SetValue(entity, id);
        if (createdAtProperty != null && createdAtProperty.CanWrite) createdAtProperty.SetValue(entity, createdAt);
        if (updatedAtProperty != null && updatedAtProperty.CanWrite) updatedAtProperty.SetValue(entity, updatedAt);

        return entity;
    }
}
