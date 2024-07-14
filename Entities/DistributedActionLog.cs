namespace N35T.Distributed;

public class DistributedActionLog {

    public DateTimeOffset Timestamp {get;set;}

    public string Table { get;set; }

    public LogAction Action { get;set; }

    public string ReferencedId { get;set; }

    public string? AffectedColumn {get;set;}
    public string? NewValue {get;set;}

    private DistributedActionLog() {
        
    }

    public DistributedActionLog(DateTimeOffset timestamp, string table, LogAction action, string refId) {
        if(action == LogAction.INSERT || action == LogAction.UPDATE) {
            throw new ArgumentException("With LogAction Insert or Update, a new value and an affected column must be specified!");
        } 
        Timestamp = timestamp;
        Table = table;
        Action = action;
        ReferencedId = refId;
    }

    public DistributedActionLog(DateTimeOffset timestamp, string table, LogAction action, string refId, string? affectedColumn, string? newVal) {
        if((action == LogAction.DELETE || action == LogAction.SYNC) && (affectedColumn != null || newVal != null)) {
            throw new ArgumentException("With LogAction Insert or Update, a new value and an affected column must not be specified!");
        } 
        Timestamp = timestamp;
        Table = table;
        Action = action;
        ReferencedId = refId;
        AffectedColumn = affectedColumn;
        NewValue = newVal;
    }
}
