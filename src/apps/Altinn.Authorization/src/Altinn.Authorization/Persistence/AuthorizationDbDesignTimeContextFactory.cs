using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Altinn.Platform.Authorization.Persistence;

/// <summary>
/// Constructs <see cref="AuthorizationDbContext"/> for the EF Core CLI
/// (<c>dotnet ef migrations ...</c>). The connection string is only used when a
/// command actually touches the database; <c>migrations add</c> does not connect.
/// </summary>
public sealed class AuthorizationDbDesignTimeContextFactory : IDesignTimeDbContextFactory<AuthorizationDbContext>
{
    /// <inheritdoc />
    public AuthorizationDbContext CreateDbContext(string[] args)
    {
        // Accept both the ':' form and the '__' form: ':' isn't a valid character
        // in environment-variable names on Linux/macOS shells, so CI/dev overrides
        // have to use the '__' form that .NET configuration providers also map.
        var connectionString = Environment.GetEnvironmentVariable("PostgreSQLSettings:AdminConnectionString")
            ?? Environment.GetEnvironmentVariable("PostgreSQLSettings__AdminConnectionString")
            ?? "Host=localhost;Port=5432;Username=platform_authorization_admin;Password=Password;Database=authorizationdb;Include Error Detail=true";

        var options = new DbContextOptionsBuilder<AuthorizationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AuthorizationDbContext(options);
    }
}
