using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace N35T.Distributed;

public interface ILogDbContext {

    public DbSet<DistributedActionLog> DistributedActionLog {get;}

    public Task<int> SaveChangesAsync();

    public Task ClearTable(string tableName);

    public IModel Model {get;}

    public DatabaseFacade Datatbase {get;}
}
