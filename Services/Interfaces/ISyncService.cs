namespace N35T.Distributed;

public interface ISyncService {

    Task SyncChangesAsync(List<DistributedActionLog> logs, bool clearLogsAfterSync = false);
}
