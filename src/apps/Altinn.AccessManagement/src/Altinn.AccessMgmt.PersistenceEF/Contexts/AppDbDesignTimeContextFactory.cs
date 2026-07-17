using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

namespace Altinn.AccessMgmt.PersistenceEF.Contexts;

/// <summary>
/// Used by cli `dotnet ef migration`
/// </summary>
public sealed class AppDbDesignTimeContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets<AppDbDesignTimeContextFactory>(optional: true)
            .Build();

        var path = "PostgreSQLSettings:AdminConnectionString";
        var cs = configuration.GetValue<string>(path);
        if (string.IsNullOrWhiteSpace(cs))
        {
            Console.WriteLine($"The configuration path '{path}' is missing or empty. Set it through an environment variable or user secrets. Trying default values.");
            cs = "Database=authorizationdb;Host=localhost;Username=platform_authorization_admin;Password=Password;Include Error Detail=true";
        }
        else
        {
            cs = string.Format(cs, configuration.GetValue<string>("PostgreSQLSettings:AuthorizationDbAdminPwd"));
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs)
            .ReplaceService<IMigrationsSqlGenerator, CustomMigrationsSqlGenerator>()
            .Options;

        return new AppDbContext(options)
        {
            AuditAccessor = new AuditAccessor()
            {
                AuditValues = new AuditValues(Guid.Empty, Guid.Empty, "design-time", DateTimeOffset.UtcNow),
            }
        };
    }
}
