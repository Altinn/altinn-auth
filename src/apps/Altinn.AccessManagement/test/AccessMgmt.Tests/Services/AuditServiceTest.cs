using System.Security.Claims;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AuditService"/> middleware — pure Moq, no database.
/// </summary>
public class AuditServiceTest
{
    private static readonly string SystemGuid = Guid.NewGuid().ToString();
    private static readonly string ClaimType = "urn:altinn:userid";

    private static (AuditService middleware, Mock<IDbAuditService> auditSvc) MakeSut()
    {
        var auditSvc = new Mock<IDbAuditService>();
        return (new AuditService(auditSvc.Object), auditSvc);
    }

    private static DefaultHttpContext MakeContext(
        Endpoint endpoint = null,
        ClaimsPrincipal user = null)
    {
        var ctx = new DefaultHttpContext();
        if (endpoint != null)
        {
            ctx.SetEndpoint(endpoint);
        }

        if (user != null)
        {
            ctx.User = user;
        }

        return ctx;
    }

    private static Endpoint MakeEndpoint(DbAuditAttribute attr = null)
    {
        var metadata = attr is null
            ? new EndpointMetadataCollection()
            : new EndpointMetadataCollection(attr);
        return new Endpoint(null, metadata, "test");
    }

    [Fact]
    public async Task InvokeAsync_NoEndpoint_CallsNextWithoutSettingAudit()
    {
        var (middleware, auditSvc) = MakeSut();
        var ctx = MakeContext();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        await middleware.InvokeAsync(ctx, next);

        nextCalled.Should().BeTrue();
        auditSvc.Verify(a => a.Set(It.IsAny<ChangeRequestOptions>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_EndpointWithoutAttribute_CallsNextWithoutSettingAudit()
    {
        var (middleware, auditSvc) = MakeSut();
        var ctx = MakeContext(endpoint: MakeEndpoint());
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        await middleware.InvokeAsync(ctx, next);

        nextCalled.Should().BeTrue();
        auditSvc.Verify(a => a.Set(It.IsAny<ChangeRequestOptions>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_EndpointWithAttribute_NoMatchingClaim_CallsNextWithoutSettingAudit()
    {
        var (middleware, auditSvc) = MakeSut();
        var attr = new DbAuditAttribute { Claim = ClaimType, System = SystemGuid };
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("other-claim", "value")]));
        var ctx = MakeContext(endpoint: MakeEndpoint(attr), user: user);
        RequestDelegate next = _ => Task.CompletedTask;

        await middleware.InvokeAsync(ctx, next);

        auditSvc.Verify(a => a.Set(It.IsAny<ChangeRequestOptions>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_EndpointWithAttribute_ClaimNotGuid_CallsNextWithoutSettingAudit()
    {
        var (middleware, auditSvc) = MakeSut();
        var attr = new DbAuditAttribute { Claim = ClaimType, System = SystemGuid };
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimType, "not-a-guid")]));
        var ctx = MakeContext(endpoint: MakeEndpoint(attr), user: user);
        RequestDelegate next = _ => Task.CompletedTask;

        await middleware.InvokeAsync(ctx, next);

        auditSvc.Verify(a => a.Set(It.IsAny<ChangeRequestOptions>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_EndpointWithAttribute_ValidGuidClaim_SetsAuditAndCallsNext()
    {
        var (middleware, auditSvc) = MakeSut();
        var userId = Guid.NewGuid();
        var attr = new DbAuditAttribute { Claim = ClaimType, System = SystemGuid };
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimType, userId.ToString())]));
        var ctx = MakeContext(endpoint: MakeEndpoint(attr), user: user);
        ChangeRequestOptions capturedOptions = null;
        auditSvc.Setup(a => a.Set(It.IsAny<ChangeRequestOptions>()))
                .Callback<ChangeRequestOptions>(o => capturedOptions = o);
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        await middleware.InvokeAsync(ctx, next);

        nextCalled.Should().BeTrue();
        auditSvc.Verify(a => a.Set(It.IsAny<ChangeRequestOptions>()), Times.Once);
        capturedOptions.Should().NotBeNull();
        capturedOptions!.ChangedBy.Should().Be(userId);
        capturedOptions.ChangedBySystem.Should().Be(Guid.Parse(SystemGuid));
    }
}
