namespace N35T.Distributed;

public interface ILogRepository {

    Task LogUpdateAsync(string tableName, string refId, string column, string newVal);
    Task LogInsertAsync(string tableName, string refId, string column, string insertedVal);
    Task LogDeleteAsync(string tableName, string refId);
    Task LogSyncAsync();
    Task ClearLocalLogsAsync();

    Task<List<DistributedActionLog>> GetAllLogsAsync();
    Task<List<DistributedActionLog>> GetAllLogsSinceAsync(DateTimeOffset timestamp);
}
