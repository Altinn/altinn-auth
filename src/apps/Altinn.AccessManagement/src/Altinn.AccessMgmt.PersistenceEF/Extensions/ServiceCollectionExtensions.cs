using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Data;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Host.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccessManagementDatabase(this IServiceCollection services, Action<AccessManagementDatabaseOptions> configureOptions)
    {
        var options = new AccessManagementDatabaseOptions(configureOptions);
        ConstantGuard.ConstantIdsAreUnique();
        services.AddScoped<ReadOnlyInterceptor>();
        services.AddScoped<IAuditAccessor, AuditAccessor>();
        services.AddScoped<ITranslationService, TranslationService>();
        services.AddScoped<AppDbContextFactory>();
        services.AddScoped(sp => sp.GetRequiredService<AppDbContextFactory>().CreateDbContext());
        services.AddSingleton<AuditMiddleware>();
        
        return options.Source switch
        {
            SourceType.App => services.AddPooledDbContextFactory<AppDbContext>((sp, options) =>
            {
                var db = sp.GetRequiredService<IAltinnDatabase>();
                var connectionString = db.CreatePgsqlConnection(SourceType.App);
                options.UseNpgsql(connectionString, ConfigureNpgsql);
            }),
            SourceType.Migration => services.AddPooledDbContextFactory<AppDbContext>((sp, options) =>
            {
                var db = sp.GetRequiredService<IAltinnDatabase>();
                var connectionString = db.CreatePgsqlConnection(SourceType.Migration);
                var configuration = sp.GetRequiredService<IConfiguration>();
                options.UseAsyncSeeding(async (dbcontext, anyChanges, ct) => await StaticDataIngest.IngestAll((AppDbContext)dbcontext, ct));
                options.UseNpgsql(connectionString, ConfigureNpgsql)
                    .ReplaceService<IMigrationsSqlGenerator, CustomMigrationsSqlGenerator>();
            }),
            _ => throw new ArgumentException("Invalid configured source must be either <App, Migration>", nameof(configureOptions)),
        };
    }

    private static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder builder) { }

    public class AccessManagementDatabaseOptions
    {
        public AccessManagementDatabaseOptions(Action<AccessManagementDatabaseOptions> configureOptions)
        {
            configureOptions(this);
        }

        public SourceType Source { get; set; } = SourceType.App;
    }
}
