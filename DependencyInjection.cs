using Microsoft.Extensions.DependencyInjection;
using N35T.Distributed.Services;

namespace N35T.Distributed;

public static class DependencyInjection {

    public static IServiceCollection AddSyncronizationServices(this IServiceCollection services) {

        services.AddTransient<ILogRepository,LogRepository>();
        services.AddTransient<ISyncService, SyncService>();

        return services;
    }

}
