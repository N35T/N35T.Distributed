using Microsoft.EntityFrameworkCore;

namespace N35T.Distributed;

internal class LogRepository : ILogRepository {

    private readonly ILogDbContext _dbContext;

    public LogRepository(ILogDbContext dbContext) {
        _dbContext = dbContext;
    }

    private async Task LogAtAsync(DateTimeOffset timestamp, string tableName, LogAction action, string refId, string? column, string? newVal) {

        var log = new DistributedActionLog(timestamp, tableName, action, refId, column, newVal);

        _dbContext.DistributedActionLog.Add(log);
        await _dbContext.SaveChangesAsync();
    }

    private Task LogAsync(string tableName, LogAction action, string refId, string? column, string? newVal) {
        return LogAtAsync(DateTime.Now, tableName, action, refId, column, newVal); 
    }

    // TODO: Can old log entries be deleted on update log?
    public Task LogUpdateAsync(string tableName, string refId, string column, string newVal) {
        return LogAsync(tableName, LogAction.UPDATE, refId, column, newVal);
    }

    public Task LogInsertAsync(string tableName, string refId, string column, string insertedVal) {
        return LogAsync(tableName, LogAction.INSERT, refId, column, insertedVal);
    }

    public Task LogDeleteAsync(string tableName, string refId) {
        return LogAsync(tableName, LogAction.DELETE, refId, null, null);
    }

    public Task LogSyncAsync() {
        return LogAsync(nameof(_dbContext.DistributedActionLog), LogAction.SYNC, "0", null, null);
    }
    
    public Task ClearLocalLogsAsync() {
        return _dbContext.ClearTable(nameof(_dbContext.DistributedActionLog));
    }

    public Task<List<DistributedActionLog>> GetAllLogsAsync() {
        return _dbContext.DistributedActionLog.ToListAsync();
    }
    public Task<List<DistributedActionLog>> GetAllLogsSinceAsync(DateTimeOffset timestamp) {
        return _dbContext.DistributedActionLog
            .Where(e => e.Timestamp >= timestamp)
            .ToListAsync();
    }
}
