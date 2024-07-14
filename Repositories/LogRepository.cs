using Microsoft.EntityFrameworkCore;

namespace N35T.Distributed;

internal class LogRepository : ILogRepository {

    private readonly ILogDbContext _dbContext;

    public LogRepository(ILogDbContext dbContext) {
        _dbContext = dbContext;
    }

    private async Task LogAt(DateTimeOffset timestamp, string tableName, LogAction action, string refId, string? column, string? newVal) {

        var log = new DistributedActionLog(timestamp, tableName, action, refId, column, newVal);

        _dbContext.DistributedActionLog.Add(log);
        await _dbContext.SaveChangesAsync();
    }

    private Task Log(string tableName, LogAction action, string refId, string? column, string? newVal) {
        return LogAt(DateTime.Now, tableName, action, refId, column, newVal); 
    }

    // TODO: Can old log entries be deleted on update log?
    public Task LogUpdate(string tableName, string refId, string column, string newVal) {
        return Log(tableName, LogAction.UPDATE, refId, column, newVal);
    }

    public Task LogInsert(string tableName, string refId, string column, string insertedVal) {
        return Log(tableName, LogAction.INSERT, refId, column, insertedVal);
    }

    public Task LogDelete(string tableName, string refId) {
        return Log(tableName, LogAction.DELETE, refId, null, null);
    }

    public Task LogSync() {
        return Log(nameof(_dbContext.DistributedActionLog), LogAction.SYNC, "0", null, null);
    }
    
    public Task ClearLocalLogs() {
        return _dbContext.ClearTable(nameof(_dbContext.DistributedActionLog));
    }

    public Task<List<DistributedActionLog>> GetAllLogs() {
        return _dbContext.DistributedActionLog.ToListAsync();
    }
    public Task<List<DistributedActionLog>> GetAllLogsSince(DateTimeOffset timestamp) {
        return _dbContext.DistributedActionLog
            .Where(e => e.Timestamp >= timestamp)
            .ToListAsync();
    }
}
