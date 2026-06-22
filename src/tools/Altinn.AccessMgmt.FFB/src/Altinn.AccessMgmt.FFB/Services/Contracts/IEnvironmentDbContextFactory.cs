using Altinn.AccessMgmt.PersistenceEF.Contexts;

namespace Altinn.AccessMgmt.FFB.Services.Contracts;

public interface IEnvironmentDbContextFactory
{
    /// <summary>All environment names in config order, including those without a connection string.</summary>
    IReadOnlyList<string> Environments { get; }

    /// <summary>Returns true if the environment has a non-empty connection string and can be used.</summary>
    bool IsConfigured(string environment);

    AppDbContext CreateContext(string environment);
}
