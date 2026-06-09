using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Service-level tests for <see cref="MaskinportenSupplierService"/> against a real
/// <see cref="AppDbContext"/> (Testcontainers Postgres via <see cref="ApiFixture"/>).
/// The external collaborators (<see cref="IConnectionService"/>, <see cref="ISingleRightsService"/>,
/// <see cref="IEntityService"/>, <see cref="IAuditAccessor"/>) are mocked; the assertions exercise the
/// service's own branching: the shared organization validation (exists / not-self / both-organizations)
/// and the supplier-assignment lifecycle (create, idempotent create, idempotent delete).
///
/// This class owns an isolated, additively-seeded set of entities so each mutating test uses a distinct
/// consumer/supplier pair and the tests stay independent of execution order.
/// </summary>
public class MaskinportenSupplierServiceTests : IClassFixture<ApiFixture>
{
    // Distinct org pairs so order-independent: each mutating test owns its own pair.
    private static readonly Guid CreateConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000001");
    private static readonly Guid CreateSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000002");
    private static readonly Guid IdempotentConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000003");
    private static readonly Guid IdempotentSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000004");
    private static readonly Guid RemoveConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000005");
    private static readonly Guid RemoveSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000006");
    private static readonly Guid NoAssignmentConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000007");
    private static readonly Guid NoAssignmentSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000008");

    // Cascade pairs: each has a supplier assignment with one delegated resource seeded below.
    private static readonly Guid CascadeOkConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000011");
    private static readonly Guid CascadeOkSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000012");
    private static readonly Guid CascadeFailConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000013");
    private static readonly Guid CascadeFailSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000014");
    private static readonly Guid CascadeNoFlagConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000015");
    private static readonly Guid CascadeNoFlagSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000016");

    // RemoveResource pairs.
    private static readonly Guid RemoveResOkConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000021");
    private static readonly Guid RemoveResOkSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000022");
    private static readonly Guid RemoveResFailConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000023");
    private static readonly Guid RemoveResFailSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000024");
    private static readonly Guid RemoveResNoAssignmentConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000025");
    private static readonly Guid RemoveResNoAssignmentSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000026");
    private static readonly Guid RemoveResNoResourceConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000027");
    private static readonly Guid RemoveResNoResourceSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000028");

    // AddResource pairs + the authenticated performer.
    private static readonly Guid Performer = Guid.Parse("2c839000-0000-0000-0000-000000000031");
    private static readonly Guid AddResNoRightsConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000032");
    private static readonly Guid AddResNoRightsSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000033");
    private static readonly Guid AddResNoConnConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000034");
    private static readonly Guid AddResNoConnSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000035");
    private static readonly Guid AddResWriteFailConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000036");
    private static readonly Guid AddResWriteFailSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000037");
    private static readonly Guid AddResOkConsumer = Guid.Parse("2c839000-0000-0000-0000-000000000038");
    private static readonly Guid AddResOkSupplier = Guid.Parse("2c839000-0000-0000-0000-000000000039");

    private const string MaskinportenResourceRefId = "maskinporten-supplier-test-resource";
    private const string NonMaskinportenResourceRefId = "non-maskinporten-supplier-test-resource";

    private static readonly Guid Person = Guid.Parse("2c839000-0000-0000-0000-0000000000f1");

    public MaskinportenSupplierServiceTests(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.EnsureSeedOnce<MaskinportenSupplierServiceTests>(db =>
        {
            db.Entities.AddRange(
                Org(CreateConsumer, "910000001"),
                Org(CreateSupplier, "910000002"),
                Org(IdempotentConsumer, "910000003"),
                Org(IdempotentSupplier, "910000004"),
                Org(RemoveConsumer, "910000005"),
                Org(RemoveSupplier, "910000006"),
                Org(NoAssignmentConsumer, "910000007"),
                Org(NoAssignmentSupplier, "910000008"),
                Org(CascadeOkConsumer, "910000011"),
                Org(CascadeOkSupplier, "910000012"),
                Org(CascadeFailConsumer, "910000013"),
                Org(CascadeFailSupplier, "910000014"),
                Org(CascadeNoFlagConsumer, "910000015"),
                Org(CascadeNoFlagSupplier, "910000016"),
                Org(RemoveResOkConsumer, "910000021"),
                Org(RemoveResOkSupplier, "910000022"),
                Org(RemoveResFailConsumer, "910000023"),
                Org(RemoveResFailSupplier, "910000024"),
                Org(RemoveResNoAssignmentConsumer, "910000025"),
                Org(RemoveResNoAssignmentSupplier, "910000026"),
                Org(RemoveResNoResourceConsumer, "910000027"),
                Org(RemoveResNoResourceSupplier, "910000028"),
                Org(Performer, "910000031"),
                Org(AddResNoRightsConsumer, "910000032"),
                Org(AddResNoRightsSupplier, "910000033"),
                Org(AddResNoConnConsumer, "910000034"),
                Org(AddResNoConnSupplier, "910000035"),
                Org(AddResWriteFailConsumer, "910000036"),
                Org(AddResWriteFailSupplier, "910000037"),
                Org(AddResOkConsumer, "910000038"),
                Org(AddResOkSupplier, "910000039"),
                new Entity
                {
                    Id = Person,
                    Name = "Maskinporten Test Person",
                    PersonIdentifier = "08900049013",
                    RefId = "08900049013",
                    TypeId = EntityTypeConstants.Person,
                    VariantId = EntityVariantConstants.Person,
                });

            // The MaskinportenSchema resource the supplier delegations point at. RemoveResource
            // requires the resource to be of type "MaskinportenSchema"; the cascade path ignores
            // the type, so the same resource serves both. "MaskinportenSchema" is a real resource
            // type name and ResourceType.Name is unique, so reuse the row if it is ever present in
            // the seeded data instead of inserting a duplicate.
            var maskinportenSchemaType = db.ResourceTypes.FirstOrDefault(rt => rt.Name == "MaskinportenSchema");
            if (maskinportenSchemaType is null)
            {
                maskinportenSchemaType = new ResourceType
                {
                    Id = Guid.Parse("2c839000-0000-0000-0000-0000000000a1"),
                    Name = "MaskinportenSchema",
                };
                db.ResourceTypes.Add(maskinportenSchemaType);
            }

            // A non-MaskinportenSchema type for the negative case (test-specific name, no domain clash).
            var otherType = new ResourceType
            {
                Id = Guid.Parse("2c839000-0000-0000-0000-0000000000a2"),
                Name = "MaskinportenSupplierTestOtherType",
            };
            db.ResourceTypes.Add(otherType);

            var resource = new Resource
            {
                Id = Guid.CreateVersion7(),
                Name = "Maskinporten Supplier Test Resource",
                Description = "Maskinporten Supplier Test Resource",
                RefId = MaskinportenResourceRefId,
                ProviderId = ProviderConstants.ResourceRegistry.Id,
                TypeId = maskinportenSchemaType.Id,
            };
            var nonMaskinportenResource = new Resource
            {
                Id = Guid.CreateVersion7(),
                Name = "Non Maskinporten Supplier Test Resource",
                Description = "Non Maskinporten Supplier Test Resource",
                RefId = NonMaskinportenResourceRefId,
                ProviderId = ProviderConstants.ResourceRegistry.Id,
                TypeId = otherType.Id,
            };
            db.Resources.AddRange(resource, nonMaskinportenResource);

            // Pre-existing supplier assignment with no delegated resources, for the remove test.
            db.Assignments.Add(new Assignment
            {
                FromId = RemoveConsumer,
                ToId = RemoveSupplier,
                RoleId = RoleConstants.Supplier.Id,
            });

            // Supplier assignments that each have one delegated resource, for the cascade tests.
            var cascadeOk = SupplierAssignment(CascadeOkConsumer, CascadeOkSupplier);
            var cascadeFail = SupplierAssignment(CascadeFailConsumer, CascadeFailSupplier);
            var cascadeNoFlag = SupplierAssignment(CascadeNoFlagConsumer, CascadeNoFlagSupplier);

            // RemoveResource: two pairs that have the resource delegated (success / clear-failure),
            // and one pair whose assignment exists but has no delegated resource.
            var removeResOk = SupplierAssignment(RemoveResOkConsumer, RemoveResOkSupplier);
            var removeResFail = SupplierAssignment(RemoveResFailConsumer, RemoveResFailSupplier);
            var removeResNoResource = SupplierAssignment(RemoveResNoResourceConsumer, RemoveResNoResourceSupplier);

            // AddResource: the write-fail and success pairs need an existing supplier connection
            // (the no-rights and no-connection pairs deliberately have none).
            var addResWriteFail = SupplierAssignment(AddResWriteFailConsumer, AddResWriteFailSupplier);
            var addResOk = SupplierAssignment(AddResOkConsumer, AddResOkSupplier);

            db.Assignments.AddRange(
                cascadeOk, cascadeFail, cascadeNoFlag,
                removeResOk, removeResFail, removeResNoResource,
                addResWriteFail, addResOk);
            db.AssignmentResources.AddRange(
                ResourceOn(cascadeOk, resource, "policies/cascade-ok.xml"),
                ResourceOn(cascadeFail, resource, "policies/cascade-fail.xml"),
                ResourceOn(cascadeNoFlag, resource, "policies/cascade-noflag.xml"),
                ResourceOn(removeResOk, resource, "policies/remove-res-ok.xml"),
                ResourceOn(removeResFail, resource, "policies/remove-res-fail.xml"));

            db.SaveChanges();
        });
    }

    private ApiFixture Fixture { get; }

    [Fact]
    public async Task AddSupplier_WithValidOrganizations_CreatesSupplierAssignment()
    {
        var result = await RunService(s => s.AddSupplier(CreateConsumer, CreateSupplier, TestContext.Current.CancellationToken));

        result.IsProblem.Should().BeFalse();

        var assignments = await SupplierAssignments(CreateConsumer, CreateSupplier);
        assignments.Should().ContainSingle();
        assignments[0].RoleId.Should().Be(RoleConstants.Supplier.Id);
        assignments[0].FromId.Should().Be(CreateConsumer);
        assignments[0].ToId.Should().Be(CreateSupplier);
    }

    [Fact]
    public async Task AddSupplier_WhenAssignmentAlreadyExists_ReturnsExistingWithoutCreatingDuplicate()
    {
        var first = await RunService(s => s.AddSupplier(IdempotentConsumer, IdempotentSupplier, TestContext.Current.CancellationToken));
        var second = await RunService(s => s.AddSupplier(IdempotentConsumer, IdempotentSupplier, TestContext.Current.CancellationToken));

        first.IsProblem.Should().BeFalse();
        second.IsProblem.Should().BeFalse();

        var assignments = await SupplierAssignments(IdempotentConsumer, IdempotentSupplier);
        assignments.Should().ContainSingle("a second AddSupplier for the same pair must not create a duplicate assignment");
    }

    [Fact]
    public async Task AddSupplier_WhenConsumerDoesNotExist_ReturnsProblem()
    {
        var result = await RunService(s => s.AddSupplier(Guid.NewGuid(), CreateSupplier, TestContext.Current.CancellationToken));

        result.IsProblem.Should().BeTrue();
    }

    [Fact]
    public async Task AddSupplier_WhenSupplierDoesNotExist_ReturnsProblem()
    {
        var result = await RunService(s => s.AddSupplier(CreateConsumer, Guid.NewGuid(), TestContext.Current.CancellationToken));

        result.IsProblem.Should().BeTrue();
    }

    [Fact]
    public async Task AddSupplier_WhenConsumerIsNotOrganization_ReturnsProblem()
    {
        var result = await RunService(s => s.AddSupplier(Person, CreateSupplier, TestContext.Current.CancellationToken));

        result.IsProblem.Should().BeTrue();
    }

    [Fact]
    public async Task AddSupplier_WhenSupplierIsNotOrganization_ReturnsProblem()
    {
        var result = await RunService(s => s.AddSupplier(CreateConsumer, Person, TestContext.Current.CancellationToken));

        result.IsProblem.Should().BeTrue();
    }

    [Fact]
    public async Task AddSupplier_WhenConsumerEqualsSupplier_ReturnsProblem()
    {
        var result = await RunService(s => s.AddSupplier(CreateConsumer, CreateConsumer, TestContext.Current.CancellationToken));

        result.IsProblem.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveSupplier_WhenAssignmentExistsWithoutResources_RemovesAssignment()
    {
        var problem = await RunService(s => s.RemoveSupplier(RemoveConsumer, RemoveSupplier, cascade: false, TestContext.Current.CancellationToken));

        problem.Should().BeNull();

        var assignments = await SupplierAssignments(RemoveConsumer, RemoveSupplier);
        assignments.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveSupplier_WhenAssignmentDoesNotExist_ReturnsNull()
    {
        var problem = await RunService(s => s.RemoveSupplier(NoAssignmentConsumer, NoAssignmentSupplier, cascade: false, TestContext.Current.CancellationToken));

        problem.Should().BeNull("removing a non-existent supplier assignment is idempotent (204)");
    }

    [Fact]
    public async Task RemoveSupplier_WhenConsumerIsNotOrganization_ReturnsProblem()
    {
        var problem = await RunService(s => s.RemoveSupplier(Person, NoAssignmentSupplier, cascade: false, TestContext.Current.CancellationToken));

        problem.Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveSupplier_WhenResourcesExistAndCascadeFalse_ReturnsProblemAndKeepsAssignment()
    {
        var problem = await RunService(s => s.RemoveSupplier(CascadeNoFlagConsumer, CascadeNoFlagSupplier, cascade: false, TestContext.Current.CancellationToken));

        problem.Should().NotBeNull("removing a supplier with delegated resources requires cascade=true");

        (await SupplierAssignments(CascadeNoFlagConsumer, CascadeNoFlagSupplier)).Should().ContainSingle();
        (await DelegatedResourceCount(CascadeNoFlagConsumer, CascadeNoFlagSupplier)).Should().Be(1);
    }

    [Fact]
    public async Task RemoveSupplier_WhenCascadeTrueAndPolicyClearSucceeds_RemovesAssignmentAndResources()
    {
        var singleRights = new Mock<ISingleRightsService>();
        singleRights
            .Setup(r => r.ClearPolicyRules(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("v2");

        var problem = await RunService(
            s => s.RemoveSupplier(CascadeOkConsumer, CascadeOkSupplier, cascade: true, TestContext.Current.CancellationToken),
            singleRights: singleRights.Object);

        problem.Should().BeNull();

        (await SupplierAssignments(CascadeOkConsumer, CascadeOkSupplier)).Should().BeEmpty();
        (await DelegatedResourceCount(CascadeOkConsumer, CascadeOkSupplier)).Should().Be(0);
    }

    [Fact]
    public async Task RemoveSupplier_WhenCascadeTrueAndPolicyClearFails_ReturnsProblemAndKeepsRecords()
    {
        var singleRights = new Mock<ISingleRightsService>();
        singleRights
            .Setup(r => r.ClearPolicyRules(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);

        var problem = await RunService(
            s => s.RemoveSupplier(CascadeFailConsumer, CascadeFailSupplier, cascade: true, TestContext.Current.CancellationToken),
            singleRights: singleRights.Object);

        problem.Should().NotBeNull("a failed policy clear must abort the cascade");

        // Failure must not delete the database records.
        (await SupplierAssignments(CascadeFailConsumer, CascadeFailSupplier)).Should().ContainSingle();
        (await DelegatedResourceCount(CascadeFailConsumer, CascadeFailSupplier)).Should().Be(1);
    }

    [Fact]
    public async Task RemoveResource_WhenResourceIsDelegated_ClearsPolicyAndRemovesLink()
    {
        var singleRights = new Mock<ISingleRightsService>();
        singleRights
            .Setup(r => r.ClearPolicyRules(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("v2");

        var problem = await RunService(
            s => s.RemoveResource(RemoveResOkConsumer, RemoveResOkSupplier, MaskinportenResourceRefId, TestContext.Current.CancellationToken),
            singleRights: singleRights.Object);

        problem.Should().BeNull();

        // The resource link is removed, but the supplier assignment itself remains.
        (await DelegatedResourceCount(RemoveResOkConsumer, RemoveResOkSupplier)).Should().Be(0);
        (await SupplierAssignments(RemoveResOkConsumer, RemoveResOkSupplier)).Should().ContainSingle();
    }

    [Fact]
    public async Task RemoveResource_WhenPolicyClearFails_ReturnsProblemAndKeepsRecord()
    {
        var singleRights = new Mock<ISingleRightsService>();
        singleRights
            .Setup(r => r.ClearPolicyRules(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null);

        var problem = await RunService(
            s => s.RemoveResource(RemoveResFailConsumer, RemoveResFailSupplier, MaskinportenResourceRefId, TestContext.Current.CancellationToken),
            singleRights: singleRights.Object);

        problem.Should().NotBeNull();

        // A failed policy clear must not delete the resource link.
        (await DelegatedResourceCount(RemoveResFailConsumer, RemoveResFailSupplier)).Should().Be(1);
    }

    [Fact]
    public async Task RemoveResource_WhenSupplierAssignmentDoesNotExist_ReturnsNull()
    {
        var problem = await RunService(s => s.RemoveResource(
            RemoveResNoAssignmentConsumer, RemoveResNoAssignmentSupplier, MaskinportenResourceRefId, TestContext.Current.CancellationToken));

        problem.Should().BeNull("removing a resource when there is no supplier assignment is idempotent (204)");
    }

    [Fact]
    public async Task RemoveResource_WhenResourceNotDelegated_ReturnsNull()
    {
        var problem = await RunService(s => s.RemoveResource(
            RemoveResNoResourceConsumer, RemoveResNoResourceSupplier, MaskinportenResourceRefId, TestContext.Current.CancellationToken));

        problem.Should().BeNull("removing a resource that was never delegated is idempotent (204)");
    }

    [Fact]
    public async Task RemoveResource_WhenResourceIsNotMaskinportenSchema_ReturnsProblem()
    {
        var problem = await RunService(s => s.RemoveResource(
            RemoveResNoAssignmentConsumer, RemoveResNoAssignmentSupplier, NonMaskinportenResourceRefId, TestContext.Current.CancellationToken));

        problem.Should().NotBeNull("only MaskinportenSchema resources are valid for supplier delegation");
    }

    [Fact]
    public async Task RemoveResource_WhenResourceDoesNotExist_ReturnsNull()
    {
        var problem = await RunService(s => s.RemoveResource(
            RemoveResNoAssignmentConsumer, RemoveResNoAssignmentSupplier, "this-resource-does-not-exist", TestContext.Current.CancellationToken));

        // Current behavior: for a non-existent resource GetResourceByRefId returns Problems.InvalidResource,
        // which is a ProblemInstance (not a ValidationProblemInstance), so RemoveResource's
        // `as ValidationProblemInstance` cast yields null and the call is treated as an idempotent no-op (204).
        // This differs from the wrong-resource-type case, which does surface a validation problem.
        problem.Should().BeNull();
    }

    [Fact]
    public async Task AddResource_WhenDelegationCheckReturnsNoDelegableRights_ReturnsProblem()
    {
        // Delegation check succeeds but no right is delegable -> not authorized, before any connection check.
        var result = await RunService(
            s => s.AddResource(AddResNoRightsConsumer, AddResNoRightsSupplier, MaskinportenResourceRefId, TestContext.Current.CancellationToken),
            connection: ConnectionReturning(("scope.read", false)),
            audit: AuditAs(Performer));

        result.IsProblem.Should().BeTrue();
    }

    [Fact]
    public async Task AddResource_WhenNoSupplierConnectionExists_ReturnsProblem()
    {
        // There are delegable rights, but no supplier assignment between the parties.
        var result = await RunService(
            s => s.AddResource(AddResNoConnConsumer, AddResNoConnSupplier, MaskinportenResourceRefId, TestContext.Current.CancellationToken),
            connection: ConnectionReturning(("scope.read", true)),
            audit: AuditAs(Performer));

        result.IsProblem.Should().BeTrue();
    }

    [Fact]
    public async Task AddResource_WhenPolicyRuleWriteFails_ReturnsProblem()
    {
        var singleRights = new Mock<ISingleRightsService>();
        singleRights
            .Setup(r => r.TryWriteDelegationPolicyRules(
                It.IsAny<Entity>(), It.IsAny<Entity>(), It.IsAny<Resource>(),
                It.IsAny<List<string>>(), It.IsAny<Entity>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { new() { CreatedSuccessfully = false } });

        var result = await RunService(
            s => s.AddResource(AddResWriteFailConsumer, AddResWriteFailSupplier, MaskinportenResourceRefId, TestContext.Current.CancellationToken),
            singleRights: singleRights.Object,
            connection: ConnectionReturning(("scope.read", true)),
            audit: AuditAs(Performer));

        result.IsProblem.Should().BeTrue();
    }

    [Fact]
    public async Task AddResource_WhenAuthorizedAndConnectionExistsAndPolicyWriteSucceeds_ReturnsTrue()
    {
        var singleRights = new Mock<ISingleRightsService>();
        singleRights
            .Setup(r => r.TryWriteDelegationPolicyRules(
                It.IsAny<Entity>(), It.IsAny<Entity>(), It.IsAny<Resource>(),
                It.IsAny<List<string>>(), It.IsAny<Entity>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { new() { CreatedSuccessfully = true } });

        var result = await RunService(
            s => s.AddResource(AddResOkConsumer, AddResOkSupplier, MaskinportenResourceRefId, TestContext.Current.CancellationToken),
            singleRights: singleRights.Object,
            connection: ConnectionReturning(("scope.read", true)),
            audit: AuditAs(Performer));

        result.IsProblem.Should().BeFalse();
        result.Value.Should().BeTrue();
    }

    private static IConnectionService ConnectionReturning(params (string Key, bool Result)[] rights)
    {
        var dto = new ResourceCheckDto
        {
            Resource = new ResourceDto(),
            Rights = rights
                .Select(r => new RightCheckDto { Right = new RightDto { Key = r.Key }, Result = r.Result })
                .ToList(),
        };

        var mock = new Mock<IConnectionService>();
        mock
            .Setup(c => c.ResourceDelegationCheck(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<Action<ConnectionOptions>>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        return mock.Object;
    }

    private static IAuditAccessor AuditAs(Guid performer)
    {
        var mock = new Mock<IAuditAccessor>();
        mock.SetupGet(a => a.AuditValues).Returns(new AuditValues(performer));
        return mock.Object;
    }

    private static Entity Org(Guid id, string orgNo) => new()
    {
        Id = id,
        Name = $"Maskinporten Test Org {orgNo}",
        OrganizationIdentifier = orgNo,
        RefId = orgNo,
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.AS,
    };

    private static Assignment SupplierAssignment(Guid consumerId, Guid supplierId) => new()
    {
        FromId = consumerId,
        ToId = supplierId,
        RoleId = RoleConstants.Supplier.Id,
    };

    private static AssignmentResource ResourceOn(Assignment assignment, Resource resource, string policyPath) => new()
    {
        AssignmentId = assignment.Id,
        ResourceId = resource.Id,
        PolicyPath = policyPath,
        PolicyVersion = "v1",
    };

    private async Task<TResult> RunService<TResult>(
        Func<MaskinportenSupplierService, Task<TResult>> act,
        ISingleRightsService singleRights = null,
        IConnectionService connection = null,
        IEntityService entity = null,
        IAuditAccessor audit = null)
    {
        using var scope = Fixture.Services.CreateEFScope(SystemEntityConstants.StaticDataIngest);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new MaskinportenSupplierService(
            db,
            audit ?? Mock.Of<IAuditAccessor>(),
            connection ?? Mock.Of<IConnectionService>(),
            singleRights ?? Mock.Of<ISingleRightsService>(),
            entity ?? Mock.Of<IEntityService>());

        return await act(service);
    }

    private async Task<List<Assignment>> SupplierAssignments(Guid consumerId, Guid supplierId)
    {
        List<Assignment> rows = null;
        await Fixture.QueryDb(async db =>
        {
            rows = await db.Assignments
                .AsNoTracking()
                .Where(a => a.FromId == consumerId && a.ToId == supplierId && a.RoleId == RoleConstants.Supplier.Id)
                .ToListAsync();
        });

        return rows;
    }

    private async Task<int> DelegatedResourceCount(Guid consumerId, Guid supplierId)
    {
        var count = 0;
        await Fixture.QueryDb(async db =>
        {
            count = await db.AssignmentResources
                .AsNoTracking()
                .CountAsync(ar =>
                    ar.Assignment.FromId == consumerId &&
                    ar.Assignment.ToId == supplierId &&
                    ar.Assignment.RoleId == RoleConstants.Supplier.Id);
        });

        return count;
    }
}
