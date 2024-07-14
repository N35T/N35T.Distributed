using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace N35T.Distributed;

public interface ILogDbContext {

    public DbSet<DistributedActionLog> DistributedActionLog {get;}

    public Task<int> SaveChangesAsync();

    public Task ClearTable(string tableName);
}
