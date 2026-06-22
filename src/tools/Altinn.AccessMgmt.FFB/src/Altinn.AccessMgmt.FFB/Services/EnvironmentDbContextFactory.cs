using Altinn.AccessMgmt.FFB.Config;
using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.FFB.Services;

public class EnvironmentDbContextFactory : IEnvironmentDbContextFactory
{
    private readonly Dictionary<string, DbContextOptions<AppDbContext>> _options;
    private readonly HashSet<string> _notConfigured;

    /// <inheritdoc/>
    public IReadOnlyList<string> Environments { get; }

    public EnvironmentDbContextFactory(IOptions<EnvironmentsConfig> config)
    {
        var entries = config.Value.Environments;
        if (entries is not { Count: > 0 })
        {
            throw new InvalidOperationException("No environments configured. Add an \"Environments\" array to appsettings.json.");
        }

        var dict = new Dictionary<string, DbContextOptions<AppDbContext>>(
            StringComparer.OrdinalIgnoreCase);
        var notConfigured = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.AccessMgmt))
            {
                notConfigured.Add(entry.Name);
                continue;
            }

            var ob = new DbContextOptionsBuilder<AppDbContext>();
            ob.UseNpgsql(entry.AccessMgmt);
            dict[entry.Name] = ob.Options;
        }

        if (dict.Count == 0)
        {
            throw new InvalidOperationException(
                "No environments have a ConnectionString configured in appsettings.json. " +
                $"At least one of the {entries.Count} configured environments must have a non-empty ConnectionString.");
        }

        _options = dict;
        _notConfigured = notConfigured;
        Environments = entries.Select(e => e.Name).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public bool IsConfigured(string environment) =>
        !_notConfigured.Contains(environment);

    /// <inheritdoc/>
    public AppDbContext CreateContext(string environment)
    {
        if (!_options.TryGetValue(environment, out var options))
        {
            if (_notConfigured.Contains(environment))
            {
                throw new InvalidOperationException($"Environment \"{environment}\" is not configured (empty ConnectionString in appsettings.json).");
            }

            throw new ArgumentException(
                $"Unknown environment \"{environment}\". Valid: {string.Join(", ", Environments)}",
                nameof(environment));
        }

        return new AppDbContext(options);
    }
}
