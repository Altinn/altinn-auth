using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Models.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="AuditMiddleware"/> — pure in-memory, no database.
/// Covers the three attribute branches that do not require DB access:
/// <see cref="AuditJWTClaimToDbAttribute"/>, <see cref="AuditStaticDbAttribute"/>
/// and <see cref="AuditPlatformStaticDbAttribute"/>. The
/// <see cref="AuditServiceOwnerConsumerAttribute"/> branch needs an
/// <c>AppDbContext</c> lookup and is left to integration tests.
/// </summary>
[UnitTest]
public class AuditServiceTest
{
    private const string SystemGuid = "00000000-0000-0000-0000-000000000001";
    private const string ChangedByGuid = "00000000-0000-0000-0000-000000000002";
    private const string ClaimType = "urn:altinn:userid";

    private static (AuditMiddleware middleware, AuditAccessor accessor, DefaultHttpContext context) MakeSut(
        Endpoint endpoint = null,
        ClaimsPrincipal user = null)
    {
        var accessor = new AuditAccessor();
        var services = new ServiceCollection();
        services.AddSingleton<IAuditAccessor>(accessor);

        var ctx = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };

        if (endpoint != null)
        {
            ctx.SetEndpoint(endpoint);
        }

        if (user != null)
        {
            ctx.User = user;
        }

        return (new AuditMiddleware(), accessor, ctx);
    }

    private static Endpoint MakeEndpoint(params object[] metadata)
        => new(null, new EndpointMetadataCollection(metadata), "test");

    private static ClaimsPrincipal Principal(params (string type, string value)[] claims)
        => new(new ClaimsIdentity(claims.Select(c => new Claim(c.type, c.value))));

    [Fact]
    public async Task InvokeAsync_NoEndpoint_DoesNotSetAuditValues_CallsNext()
    {
        var (middleware, accessor, ctx) = MakeSut();
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        await middleware.InvokeAsync(ctx, next);

        nextCalled.Should().BeTrue();
        accessor.AuditValues.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_EndpointWithoutAuditAttribute_DoesNotSetAuditValues()
    {
        var (middleware, accessor, ctx) = MakeSut(endpoint: MakeEndpoint());

        await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

        accessor.AuditValues.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_JwtClaim_MatchingValidGuid_SetsAuditValues()
    {
        var userId = Guid.NewGuid();
        var attr = new AuditJWTClaimToDbAttribute { Claim = ClaimType, System = SystemGuid };
        var (middleware, accessor, ctx) = MakeSut(
            endpoint: MakeEndpoint(attr),
            user: Principal((ClaimType, userId.ToString())));

        await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

        accessor.AuditValues.Should().NotBeNull();
        accessor.AuditValues!.ChangedBy.Should().Be(userId);
        accessor.AuditValues.ChangedBySystem.Should().Be(Guid.Parse(SystemGuid));
    }

    [Fact]
    public async Task InvokeAsync_JwtClaim_NoMatchingClaim_NoSystemUserClaim_DoesNotSetAuditValues()
    {
        var attr = new AuditJWTClaimToDbAttribute { Claim = ClaimType, System = SystemGuid, AllowSystemUser = false };
        var (middleware, accessor, ctx) = MakeSut(
            endpoint: MakeEndpoint(attr),
            user: Principal(("other-claim", Guid.NewGuid().ToString())));

        await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

        accessor.AuditValues.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_JwtClaim_ClaimNotGuid_DoesNotSetAuditValues()
    {
        var attr = new AuditJWTClaimToDbAttribute { Claim = ClaimType, System = SystemGuid, AllowSystemUser = false };
        var (middleware, accessor, ctx) = MakeSut(
            endpoint: MakeEndpoint(attr),
            user: Principal((ClaimType, "not-a-guid")));

        await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

        accessor.AuditValues.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_JwtClaim_FallsBackToSystemUserClaim_WhenStandardClaimMissing()
    {
        var systemUserId = Guid.NewGuid();
        var systemUserClaim = new SystemUserClaim
        {
            Systemuser_id = [systemUserId.ToString()],
        };
        var attr = new AuditJWTClaimToDbAttribute { Claim = ClaimType, System = SystemGuid, AllowSystemUser = true };
        var (middleware, accessor, ctx) = MakeSut(
            endpoint: MakeEndpoint(attr),
            user: Principal(("authorization_details", JsonSerializer.Serialize(systemUserClaim))));

        await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

        accessor.AuditValues.Should().NotBeNull();
        accessor.AuditValues!.ChangedBy.Should().Be(systemUserId);
        accessor.AuditValues.ChangedBySystem.Should().Be(Guid.Parse(SystemGuid));
    }

    [Fact]
    public async Task InvokeAsync_StaticDb_BothChangedByAndSystem_SetsBoth()
    {
        var attr = new AuditStaticDbAttribute { ChangedBy = ChangedByGuid, System = SystemGuid };
        var (middleware, accessor, ctx) = MakeSut(endpoint: MakeEndpoint(attr));

        await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

        accessor.AuditValues.Should().NotBeNull();
        accessor.AuditValues!.ChangedBy.Should().Be(Guid.Parse(ChangedByGuid));
        accessor.AuditValues.ChangedBySystem.Should().Be(Guid.Parse(SystemGuid));
    }

    [Fact]
    public async Task InvokeAsync_StaticDb_SystemOnly_UsesSystemForBothFields()
    {
        var attr = new AuditStaticDbAttribute { System = SystemGuid };
        var (middleware, accessor, ctx) = MakeSut(endpoint: MakeEndpoint(attr));

        await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

        accessor.AuditValues.Should().NotBeNull();
        accessor.AuditValues!.ChangedBy.Should().Be(Guid.Parse(SystemGuid));
        accessor.AuditValues.ChangedBySystem.Should().Be(Guid.Parse(SystemGuid));
    }

    [Fact]
    public async Task InvokeAsync_PlatformStaticDb_BothChangedByAndSystem_SetsBoth()
    {
        var attr = new AuditPlatformStaticDbAttribute { ChangedBy = ChangedByGuid, System = SystemGuid };
        var (middleware, accessor, ctx) = MakeSut(endpoint: MakeEndpoint(attr));

        await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

        accessor.AuditValues.Should().NotBeNull();
        accessor.AuditValues!.ChangedBy.Should().Be(Guid.Parse(ChangedByGuid));
        accessor.AuditValues.ChangedBySystem.Should().Be(Guid.Parse(SystemGuid));
    }

    [Fact]
    public async Task InvokeAsync_PlatformStaticDb_SystemOnly_UsesSystemForBothFields()
    {
        var attr = new AuditPlatformStaticDbAttribute { System = SystemGuid };
        var (middleware, accessor, ctx) = MakeSut(endpoint: MakeEndpoint(attr));

        await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

        accessor.AuditValues.Should().NotBeNull();
        accessor.AuditValues!.ChangedBy.Should().Be(Guid.Parse(SystemGuid));
        accessor.AuditValues.ChangedBySystem.Should().Be(Guid.Parse(SystemGuid));
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNext_RegardlessOfAttribute()
    {
        var attr = new AuditStaticDbAttribute { System = SystemGuid };
        var (middleware, _, ctx) = MakeSut(endpoint: MakeEndpoint(attr));
        var nextCalled = false;

        await middleware.InvokeAsync(ctx, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }
}
