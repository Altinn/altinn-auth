using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Constants;

[UnitTest]
[Collection(nameof(ConstantLookupIsProdCollection))]
public sealed class ConstantLookupIsProdTest : IDisposable
{
    private readonly bool _isProd = ConstantLookup.IsProd;

    public void Dispose() => ConstantLookup.IsProd = _isProd;

    [Fact]
    public void Lookups_OutsideProd_FindTestOnlyConstant()
    {
        ConstantLookup.IsProd = false;
        var testOnly = TestPackages.TestOnlyPackage;

        Assert.True(ConstantLookup.TryGetById<Package>(typeof(TestPackages), testOnly.Id, out _));
        Assert.True(ConstantLookup.TryGetByUrn<Package>(typeof(TestPackages), testOnly.Entity.Urn, out _));
        Assert.True(ConstantLookup.TryGetByCode<Package>(typeof(TestPackages), testOnly.Entity.Code, out _));
        Assert.Contains(testOnly, ConstantLookup.AllEntities<Package>(typeof(TestPackages)));
        Assert.True(ConstantLookup.IsAvailable<Package>(typeof(TestPackages), testOnly.Id));
    }

    [Fact]
    public void Lookups_InProd_LeaveOutTestOnlyConstant()
    {
        ConstantLookup.IsProd = true;
        var testOnly = TestPackages.TestOnlyPackage;

        Assert.False(ConstantLookup.TryGetById<Package>(typeof(TestPackages), testOnly.Id, out var byId));
        Assert.Null(byId);
        Assert.False(ConstantLookup.TryGetByUrn<Package>(typeof(TestPackages), testOnly.Entity.Urn, out _));
        Assert.False(ConstantLookup.TryGetByCode<Package>(typeof(TestPackages), testOnly.Entity.Code, out _));
        Assert.False(ConstantLookup.IsAvailable<Package>(typeof(TestPackages), testOnly.Id));

        var all = ConstantLookup.AllEntities<Package>(typeof(TestPackages));
        Assert.DoesNotContain(testOnly, all);
        Assert.Contains(TestPackages.Regular, all);
        Assert.True(ConstantLookup.TryGetById<Package>(typeof(TestPackages), TestPackages.Regular.Id, out _));
        Assert.True(ConstantLookup.IsAvailable<Package>(typeof(TestPackages), TestPackages.Regular.Id));
    }

    [Fact]
    public void TaxBaseExtendedAccess_IsLeftOutInProdOnly()
    {
        const string urn = "urn:altinn:accesspackage:skattegrunnlag-utvidet-tilgang";

        ConstantLookup.IsProd = false;
        Assert.True(PackageConstants.TryGetByUrn(urn, out var package));
        Assert.Equal(PackageConstants.TaxBaseExtendedAccess, package);

        ConstantLookup.IsProd = true;
        Assert.False(PackageConstants.TryGetByUrn(urn, out _));
        Assert.False(PackageConstants.IsAvailable(PackageConstants.TaxBaseExtendedAccess.Id));
    }

    [Fact]
    public void IsAvailable_WithIdThatIsNotAConstant_ReturnsTrue()
    {
        ConstantLookup.IsProd = true;

        Assert.True(ConstantLookup.IsAvailable<Package>(typeof(TestPackages), Guid.CreateVersion7()));
    }

    private static class TestPackages
    {
        public static ConstantDefinition<Package> Regular { get; } = new("0f5c8a3e-1b7d-4c2a-9e6f-000000000001")
        {
            Entity = new() { Name = "Regular", Urn = "urn:altinn:accesspackage:test-regular", Code = "test-regular" },
        };

        public static ConstantDefinition<Package> TestOnlyPackage { get; } = new("0f5c8a3e-1b7d-4c2a-9e6f-000000000002")
        {
            Entity = new() { Name = "Test only", Urn = "urn:altinn:accesspackage:test-only", Code = "test-only" },
            TestOnly = true,
        };
    }
}

/// <summary>
/// Runs the tests that change <see cref="ConstantLookup.IsProd"/> on their own, since the whole process shares it.
/// </summary>
[CollectionDefinition(nameof(ConstantLookupIsProdCollection), DisableParallelization = true)]
public class ConstantLookupIsProdCollection
{
}
