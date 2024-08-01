using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace N35T.Distributed.Services;

internal class SyncService : ISyncService {

    private readonly ILogDbContext _dbContext;
    private readonly ILogRepository _logRepo;

    public SyncService(ILogDbContext dbContext, ILogRepository logRepo) {
        _dbContext = dbContext;
        _logRepo = logRepo;
    }

    public async Task SyncChangesAsync(List<DistributedActionLog> logs, bool clearLogsAfterSync = false) {
        using var transaction = _dbContext.Datatbase.BeginTransaction();

        try {
            var tasks = new List<Task>(logs.Count);

            var groupedInsertLogs = logs.Where(e => e.Action == LogAction.INSERT).GroupBy(e => new { e.Table, e.ReferencedId});
            foreach(var grouping in groupedInsertLogs) {
                tasks.Add(HandleInsertStatement([.. grouping]));
            }

            var updateLogs = logs.Where(e => e.Action == LogAction.UPDATE);
            foreach(var log in updateLogs) {
                tasks.Add(HandleUpdateStatement(log));
            }

            var deleteLogs = logs.Where(e => e.Action == LogAction.DELETE);
            foreach(var log in deleteLogs) {
                tasks.Add(HandleDeleteStatement(log));
            }

            await Task.WhenAll(tasks);

            if(clearLogsAfterSync) {
                await _logRepo.ClearLocalLogsAsync();
            }

            transaction.Commit();
        }catch(Exception) {
            transaction.Rollback();
            throw;
        }
    }

    private async Task HandleInsertStatement(List<DistributedActionLog> insertLogs) {
        if(insertLogs.Count == 0) {
            return;
        }
        var tableName = insertLogs[0].Table;
        var id = insertLogs[0].ReferencedId;
        var dbSetProperty = GetDbSetProperty(tableName);

        var entityType = dbSetProperty.PropertyType.GetGenericArguments()[0];
        var entity = Activator.CreateInstance(entityType)
            ?? throw new SynchronizationException($"Could not instantiate type {entityType.FullName}");

        SetKeyProperty(tableName, entity, id);

        var entityProperties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !Attribute.IsDefined(p, typeof(NotMappedAttribute))).ToList();
        
        if(insertLogs.Count > entityProperties.Count) {
            throw new SynchronizationException($"Found insert log statement counts for id {id} in table {tableName} does not fit the amount of properties defined in {entityType.FullName}. Found {insertLogs.Count}, Expected {entityProperties.Count}");
        }

        foreach(var log in insertLogs) {
            var property = entityProperties.Find(p => p.Name.Equals(log.AffectedColumn, StringComparison.OrdinalIgnoreCase))
                ?? throw new SynchronizationException($"Cannot find property {log.AffectedColumn} in {entityType.FullName}");
            
            var convertedValue = ConvertType(log.NewValue, property.PropertyType);
            property.SetValue(entity, convertedValue);
        }

        foreach(var property in entityProperties) {
            var nullable = Nullable.GetUnderlyingType(property.PropertyType) != null;
            var required = Attribute.IsDefined(property, typeof(RequiredAttribute));
            var propValue = property.GetValue(entity);
            if((required || !nullable) && propValue is null) {
                throw new SynchronizationException($"The required property {property.Name} of type {entityType.FullName} was not set in a Insertion Log Statement");
            }
        }

        var dbSet = GetDbSet(dbSetProperty);
        dbSet.GetType().GetMethod("Add")!.Invoke(dbSet, [entity]);

        await _dbContext.SaveChangesAsync();
    }


    private async Task HandleUpdateStatement(DistributedActionLog updateLog) {
        if(updateLog.Action is not LogAction.UPDATE) {
            throw new ArgumentException("Log action must be UPDATE here");
        }
        if(updateLog.AffectedColumn is null) {
            throw new SynchronizationException("Found an update action with null value in the affected Column");
        }
        var entity = FindEntity(updateLog.Table, updateLog.ReferencedId);

        SetProperty(entity, updateLog.Table, updateLog.AffectedColumn, updateLog.NewValue);

        await _dbContext.SaveChangesAsync();
    }

    private async Task HandleDeleteStatement(DistributedActionLog deleteLog) {
        if(deleteLog.Action is not LogAction.DELETE) {
            throw new ArgumentException("Log action must be DELETE here");
        }
        var dbSetProp = GetDbSetProperty(deleteLog.Table);
        var dbSet = GetDbSet(dbSetProp);
        var entity = FindEntity(deleteLog.Table, deleteLog.ReferencedId, dbSetProp);

        dbSet.GetType().GetMethod("Remove")!.Invoke(dbSet, [ entity ]);

        await _dbContext.SaveChangesAsync();
    }

    private object? ConvertType(string? value , Type type) {
        return string.IsNullOrEmpty(value) ? null : Convert.ChangeType(value, type);
    }

    private PropertyInfo GetDbSetProperty(string tableName) {
        return _dbContext.GetType().GetProperties()
            .FirstOrDefault(p => p is not null
                                && p.PropertyType.IsGenericType
                                && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
                                && p.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase), null)
            ?? throw new SynchronizationException($"Cannot find table with name {tableName}");
    }

    private object GetDbSet(PropertyInfo dbSetProperty) {
        return dbSetProperty.GetValue(_dbContext) ?? throw new SynchronizationException($"Table {dbSetProperty.Name} is null in the context");
    }

    private IProperty GetKeyProperty(Type entityType, string tableName)  {
        var entityContextType = _dbContext.Model.FindEntityType(entityType)
            ?? throw new SynchronizationException($"Context does not have an entity with name ${tableName}");
        var keyProperties = (entityContextType.FindPrimaryKey()?.Properties) 
            ?? throw new SynchronizationException($"Could not find primary Key of table {tableName}");
        if (keyProperties.Count != 1) {
            throw new SynchronizationException($"Synchronization can only work with atomic primary keys. The table {tableName} has {keyProperties.Count} primary key properties!");
        }

        return keyProperties[0];
    }

    private void SetKeyProperty(string tableName, object entity, string value) {
        var entityType = entity.GetType();
        var keyProperty = GetKeyProperty(entityType, tableName);
        var keyName = keyProperty.Name;

        var keyValue = Convert.ChangeType(value, keyProperty.ClrType);
        var entityKeyProperty = entityType.GetProperty(keyName)
            ?? throw new SynchronizationException($"Cannot find property {keyName} in {entityType.FullName}");
        entityKeyProperty.SetValue(entity, keyValue);
    }

    private object FindEntity(string tableName, string id, PropertyInfo? dbSetProp = null) {
        var dbSetProperty = dbSetProp ?? GetDbSetProperty(tableName);
        var dbSet = GetDbSet(dbSetProperty);
        
        var entityType = dbSetProperty.PropertyType.GetGenericArguments()[0];
        var keyProperty = GetKeyProperty(entityType, tableName);


        var keyValue = Convert.ChangeType(id, keyProperty.ClrType);
        return dbSet.GetType().GetMethod("Find")!.Invoke(dbSet, [keyValue]) 
            ?? throw new SynchronizationException($"Entity with id {keyValue} could not be found in table {tableName}");
    }

    private void SetProperty(Object targetEntity, string tableName, string propertyName, string? value) {
        var columnProperty = targetEntity.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
            ?? throw new SynchronizationException($"Table {tableName} does not have a column with name ${propertyName}");
        var convertedValue = ConvertType(value, columnProperty.PropertyType);
        columnProperty.SetValue(targetEntity, convertedValue);
    }
}