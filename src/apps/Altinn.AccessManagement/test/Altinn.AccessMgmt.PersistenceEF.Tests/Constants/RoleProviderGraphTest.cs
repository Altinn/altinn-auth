using Altinn.AccessMgmt.PersistenceEF.Constants;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Constants;

/// <summary>
/// ConnectionEntityEnricher resolves roles from <see cref="RoleConstants"/> instead of the
/// database, and rebuilds the role -> provider -> provider type graph that the previous query
/// produced with its includes. That graph reaches the API: RoleDto carries Provider, and
/// ProviderDto carries Type.
///
/// The constants only hold foreign keys, so the projection can only fill those references if
/// every key actually resolves. A role pointing at a provider that is not a constant would
/// silently produce a null Provider in the response rather than an error, so it is checked here.
/// </summary>
[UnitTest]
public class RoleProviderGraphTest
{
    [Fact]
    public void EveryRole_ResolvesItsProvider()
    {
        var unresolved = RoleConstants.AllEntities()
            .Where(role => !ProviderConstants.TryGetById(role.Entity.ProviderId, out _))
            .Select(role => $"{role.Entity.Code} -> ProviderId {role.Entity.ProviderId}")
            .ToList();

        Assert.True(
            unresolved.Count == 0,
            $"{unresolved.Count} role(s) reference a provider that is not in ProviderConstants:"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, unresolved)}");
    }

    [Fact]
    public void EveryProvider_ResolvesItsType()
    {
        var unresolved = ProviderConstants.AllEntities()
            .Where(provider => !ProviderTypeConstants.TryGetById(provider.Entity.TypeId, out _))
            .Select(provider => $"{provider.Entity.Code} -> TypeId {provider.Entity.TypeId}")
            .ToList();

        Assert.True(
            unresolved.Count == 0,
            $"{unresolved.Count} provider(s) reference a type that is not in ProviderTypeConstants:"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, unresolved)}");
    }
}
