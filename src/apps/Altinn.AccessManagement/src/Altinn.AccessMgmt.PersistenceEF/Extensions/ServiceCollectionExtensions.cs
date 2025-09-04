using System.ComponentModel.Design;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.Authorization.Host.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccessManagementDatabase(this IServiceCollection services, Action<AccessManagementDatabaseOptions> configureOptions)
    {
        var options = new AccessManagementDatabaseOptions(configureOptions);
        services.AddScoped<ReadOnlyInterceptor>();
        return options.Source switch
        {
            SourceType.App => services.AddDbContextPool<AppDbContext>((sp, options) =>
            {
                var db = sp.GetRequiredService<IAltinnDatabase>();
                var connectionString = db.CreatePgsqlConnection(SourceType.App);
                options.UseNpgsql(connectionString, ConfigureNpgsql);
            }),
            SourceType.Migration => services.AddDbContext<AppDbContext>((sp, options) =>
            {
                var db = sp.GetRequiredService<IAltinnDatabase>();
                var connectionString = db.CreatePgsqlConnection(SourceType.Migration);
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
