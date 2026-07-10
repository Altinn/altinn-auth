using System.Security.Claims;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessMgmt.Core.Authorization;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Authorization;

/// <summary>
/// Pure-unit tests for <see cref="PersonAccessManagerHandler"/> — the
/// AuthorizationHandler that denies access when an access manager for a person
/// attempts to access the "from-others" direction (incoming connections).
/// </summary>
[UnitTest]
public class PersonAccessManagerHandlerTest
{
    private static readonly Guid PersonPartyId = Guid.Parse("3f36376b-a013-442d-a84c-3d98c3ffb2a7");
    private static readonly Guid OrgPartyId = Guid.Parse("17edfbcd-34e0-4ce4-9f90-eff3747c1234");
    private static readonly Guid AccessManagerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private sealed class StubAccessor(HttpContext? context) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = context;
    }

    private sealed class StubEntityService(Entity? entity) : IEntityService
    {
        public ValueTask<Entity> GetEntity(Guid id, CancellationToken cancellationToken) =>
            new(entity);

        public Task<bool> CreateEntity(Entity entity, CancellationToken cancellationToken) => throw new NotImplementedException();

        public ValueTask<Entity> GetOrCreateEntity(Guid id, string name, string refId, string type, string variant, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Entity> GetByOrgNo(string orgNo, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Entity> GetByOrgNoWithType(string orgNo, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Entity> GetByPersNo(string persNo, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Entity> GetByPartyId(string partyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Entity> GetByPartyId(int partyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Entity> GetByUserId(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Entity> GetByUserId(int userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Entity> GetByUsername(string username, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Entity> GetParent(Guid parentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IEnumerable<Entity>> GetChildren(Guid parentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IEnumerable<Entity>> GetChildren(IEnumerable<Guid> parentIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IEnumerable<Entity>> GetEntities(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IEnumerable<Entity>> GetEntitiesByPartyIds(IEnumerable<int> partyIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private static (PersonAccessManagerHandler Handler, DefaultHttpContext HttpContext) CreateSut(
        string queryString,
        Guid userPartyUuid,
        Entity? partyEntity)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString(queryString);

        if (userPartyUuid != Guid.Empty)
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(AltinnCoreClaimTypes.PartyUuid, userPartyUuid.ToString())
            ], "mock"));
        }

        var accessor = new StubAccessor(ctx);
        var entityService = new StubEntityService(partyEntity);
        var handler = new PersonAccessManagerHandler(accessor, entityService);
        return (handler, ctx);
    }

    private static AuthorizationHandlerContext MakeContext(ClaimsPrincipal? user = null)
    {
        var requirement = new PersonAccessManagerRequirement();
        return new AuthorizationHandlerContext([requirement], user ?? new ClaimsPrincipal(new ClaimsIdentity()), null);
    }

    [Fact]
    public async Task NoPartyParam_Succeeds()
    {
        var (handler, httpCtx) = CreateSut("?from=urn:abc", Guid.Empty, null);
        var ctx = MakeContext(httpCtx.User);

        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
    }

    [Fact]
    public async Task NoToParam_Succeeds()
    {
        var (handler, httpCtx) = CreateSut($"?party={PersonPartyId}&from={PersonPartyId}", Guid.Empty, null);
        var ctx = MakeContext(httpCtx.User);

        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
    }

    [Fact]
    public async Task ToOthersDirection_PartyEqualsFrom_Succeeds()
    {
        var (handler, httpCtx) = CreateSut($"?party={PersonPartyId}&from={PersonPartyId}&to={OrgPartyId}", AccessManagerId, null);
        var ctx = MakeContext(httpCtx.User);

        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
    }

    [Fact]
    public async Task FromOthersDirection_UserIsParty_Succeeds()
    {
        // User is the person themselves (acting as self) - should be allowed
        var personEntity = new Entity { Id = PersonPartyId, TypeId = EntityTypeConstants.Person.Id };
        var (handler, httpCtx) = CreateSut($"?party={PersonPartyId}&to={PersonPartyId}", PersonPartyId, personEntity);
        var ctx = MakeContext(httpCtx.User);

        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
    }

    [Fact]
    public async Task FromOthersDirection_AccessManagerForPerson_Fails()
    {
        // Access manager for a person attempting from-others direction - should be denied
        var personEntity = new Entity { Id = PersonPartyId, TypeId = EntityTypeConstants.Person.Id };
        var (handler, httpCtx) = CreateSut($"?party={PersonPartyId}&to={PersonPartyId}", AccessManagerId, personEntity);
        var ctx = MakeContext(httpCtx.User);

        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasFailed);
    }

    [Fact]
    public async Task FromOthersDirection_AccessManagerForOrganization_Succeeds()
    {
        // Access manager for an organization - bidirectional access should remain
        var orgEntity = new Entity { Id = OrgPartyId, TypeId = EntityTypeConstants.Organization.Id };
        var (handler, httpCtx) = CreateSut($"?party={OrgPartyId}&to={OrgPartyId}", AccessManagerId, orgEntity);
        var ctx = MakeContext(httpCtx.User);

        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
    }

    [Fact]
    public async Task FromOthersDirection_EntityNotFound_Fails()
    {
        // If entity lookup returns null, fail closed (deny access)
        var (handler, httpCtx) = CreateSut($"?party={PersonPartyId}&to={PersonPartyId}", AccessManagerId, null);
        var ctx = MakeContext(httpCtx.User);

        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasFailed);
    }

    [Fact]
    public async Task FromOthersDirection_NoUserPartyUuidClaim_AndPartyIsPerson_Fails()
    {
        // No PartyUuid claim means user cannot be the party → treated as access manager
        var personEntity = new Entity { Id = PersonPartyId, TypeId = EntityTypeConstants.Person.Id };
        var (handler, httpCtx) = CreateSut($"?party={PersonPartyId}&to={PersonPartyId}", Guid.Empty, personEntity);
        var ctx = MakeContext(httpCtx.User);

        await handler.HandleAsync(ctx);

        Assert.True(ctx.HasFailed);
    }
}
