namespace N35T.Distributed;

public interface ILoggingEntity {

    void ApplyLoggedChanges(string changedColumn, string newValue);
}
