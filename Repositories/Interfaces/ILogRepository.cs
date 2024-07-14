namespace N35T.Distributed;

public interface ILogRepository {

    Task LogUpdate(string tableName, string refId, string column, string newVal);
    Task LogInsert(string tableName, string refId, string column, string insertedVal);
    Task LogDelete(string tableName, string refId);
    Task LogSync();
    Task ClearLocalLogs();

    Task<List<DistributedActionLog>> GetAllLogs();
    Task<List<DistributedActionLog>> GetAllLogsSince(DateTimeOffset timestamp);
}
