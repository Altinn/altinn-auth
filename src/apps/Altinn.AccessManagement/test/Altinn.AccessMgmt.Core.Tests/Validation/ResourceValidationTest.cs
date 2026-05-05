using Altinn.AccessManagement.Core.Enums.ResourceRegistry;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Tests.Validation;

/// <summary>
/// Direct unit tests for the internal validation-rule factory methods in <see cref="ResourceValidation"/>.
/// </summary>
public class ResourceValidationTest
{
    // ── helpers ──────────────────────────────────────────────────────────────
    private static bool Fails(RuleExpression rule) => rule() is not null;

    private static bool Passes(RuleExpression rule) => rule() is null;

    // ── ResourceExists ───────────────────────────────────────────────────────
    [Fact]
    public void ResourceExists_NonNullResource_Passes()
    {
        var resource = new Resource();
        Passes(ResourceValidation.ResourceExists(resource, "my-resource")).Should().BeTrue();
    }

    [Fact]
    public void ResourceExists_NullResource_Fails()
    {
        Fails(ResourceValidation.ResourceExists(null, "my-resource")).Should().BeTrue();
    }

    // ── AuthorizeResourceAssignment ──────────────────────────────────────────
    [Fact]
    public void AuthorizeResourceAssignment_AllResultTrue_Passes()
    {
        var resources = new[]
        {
            new ResourceDto.ResourceDtoCheck { Resource = new ResourceDto { RefId = "r1" }, Result = true },
            new ResourceDto.ResourceDtoCheck { Resource = new ResourceDto { RefId = "r2" }, Result = true },
        };
        Passes(ResourceValidation.AuthorizeResourceAssignment(resources)).Should().BeTrue();
    }

    [Fact]
    public void AuthorizeResourceAssignment_AnyResultFalse_Fails()
    {
        var resources = new[]
        {
            new ResourceDto.ResourceDtoCheck { Resource = new ResourceDto { RefId = "r1" }, Result = true },
            new ResourceDto.ResourceDtoCheck { Resource = new ResourceDto { RefId = "r2" }, Result = false },
        };
        Fails(ResourceValidation.AuthorizeResourceAssignment(resources)).Should().BeTrue();
    }

    // ── PackageIsAssignableToRecipient ───────────────────────────────────────
    [Fact]
    public void PackageIsAssignableToRecipient_OrgWithMainAdminPackage_Fails()
    {
        var toEntity = new EntityType { Id = EntityTypeConstants.Organization };
        var urns = new[] { PackageConstants.MainAdministrator.Entity.Urn };
        Fails(ResourceValidation.PackageIsAssignableToRecipient(urns, toEntity)).Should().BeTrue();
    }

    [Fact]
    public void PackageIsAssignableToRecipient_OrgWithSafePackage_Passes()
    {
        var toEntity = new EntityType { Id = EntityTypeConstants.Organization };
        var urns = new[] { "urn:altinn:accesspackage:some-other-package" };
        Passes(ResourceValidation.PackageIsAssignableToRecipient(urns, toEntity)).Should().BeTrue();
    }

    [Fact]
    public void PackageIsAssignableToRecipient_PersonWithMainAdminPackage_Passes()
    {
        var toEntity = new EntityType { Id = EntityTypeConstants.Person };
        var urns = new[] { PackageConstants.MainAdministrator.Entity.Urn };
        Passes(ResourceValidation.PackageIsAssignableToRecipient(urns, toEntity)).Should().BeTrue();
    }

    // ── PackageUrnLookup ─────────────────────────────────────────────────────
    [Fact]
    public void PackageUrnLookup_EmptyLookupResult_Fails()
    {
        var packages = Array.Empty<Package>();
        var names = new[] { "some-package" };
        Fails(ResourceValidation.PackageUrnLookup(packages, names)).Should().BeTrue();
    }

    [Fact]
    public void PackageUrnLookup_CountMismatch_Fails()
    {
        var packages = new[] { new Package { Name = "pkg-a" } };
        var names = new[] { "pkg-a", "pkg-b" };
        Fails(ResourceValidation.PackageUrnLookup(packages, names)).Should().BeTrue();
    }

    [Fact]
    public void PackageUrnLookup_ExactMatch_Passes()
    {
        var packages = new[] { new Package { Name = "pkg-a" } };
        var names = new[] { "pkg-a" };
        Passes(ResourceValidation.PackageUrnLookup(packages, names)).Should().BeTrue();
    }

    // ── HasAssignedResources ─────────────────────────────────────────────────
    [Fact]
    public void HasAssignedResources_EmptyList_Passes()
    {
        Passes(ResourceValidation.HasAssignedResources(Array.Empty<AssignmentResource>().ToList().AsReadOnly())).Should().BeTrue();
    }

    [Fact]
    public void HasAssignedResources_NonEmptyList_Fails()
    {
        var list = new List<AssignmentResource> { new AssignmentResource() }.AsReadOnly();
        Fails(ResourceValidation.HasAssignedResources(list)).Should().BeTrue();
    }

    // ── ResourceTypeIs ───────────────────────────────────────────────────────
    [Fact]
    public void ResourceTypeIs_TypeNameMatches_Passes()
    {
        var resource = new Resource { Type = new ResourceType { Name = "GenericAccessResource" } };
        Passes(ResourceValidation.ResourceTypeIs(resource, "GenericAccessResource")).Should().BeTrue();
    }

    [Fact]
    public void ResourceTypeIs_TypeNameMismatch_Fails()
    {
        var resource = new Resource { Type = new ResourceType { Name = "OtherType" } };
        Fails(ResourceValidation.ResourceTypeIs(resource, "GenericAccessResource")).Should().BeTrue();
    }

    [Fact]
    public void ResourceTypeIs_NullResource_Fails()
    {
        Fails(ResourceValidation.ResourceTypeIs(null, "GenericAccessResource")).Should().BeTrue();
    }

    // ── PolicyClearFailed ────────────────────────────────────────────────────
    [Fact]
    public void PolicyClearFailed_VersionNotNull_Passes()
    {
        Passes(ResourceValidation.PolicyClearFailed("v1", "some-resource")).Should().BeTrue();
    }

    [Fact]
    public void PolicyClearFailed_VersionNull_Fails()
    {
        Fails(ResourceValidation.PolicyClearFailed(null, "some-resource")).Should().BeTrue();
    }

    // ── PolicyCascadeClearFailed ─────────────────────────────────────────────
    [Fact]
    public void PolicyCascadeClearFailed_VersionNotNull_Passes()
    {
        Passes(ResourceValidation.PolicyCascadeClearFailed("v2")).Should().BeTrue();
    }

    [Fact]
    public void PolicyCascadeClearFailed_VersionNull_Fails()
    {
        Fails(ResourceValidation.PolicyCascadeClearFailed(null)).Should().BeTrue();
    }
}
