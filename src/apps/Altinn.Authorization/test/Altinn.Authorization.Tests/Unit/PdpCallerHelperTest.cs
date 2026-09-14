using System.Security.Claims;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.Telemetry;

namespace Altinn.Authorization.Tests.Unit;

/// <summary>
/// Covers how <see cref="PdpCallerHelper"/> classifies the caller behind an external PDP request.
/// The classification decides who a decision is billed to, so a malformed or unexpected consumer
/// claim must fall back to owner attribution rather than silently exempting the caller.
/// </summary>
[UnitTest]
public class PdpCallerHelperTest
{
    private const string DigdirOrgNumber = "991825827";

    private static ClaimsPrincipal PrincipalWithConsumer(string consumerClaimValue)
    {
        ClaimsIdentity identity = new("mock-org");
        identity.AddClaim(new Claim("consumer", consumerClaimValue));
        return new ClaimsPrincipal(identity);
    }

    private static string ConsumerClaim(string orgNumber) =>
        $$"""{"authority":"iso6523-actorid-upis","ID":"0192:{{orgNumber}}"}""";

    [Fact]
    public void GetExternalCallerKind_DigdirConsumer_ReturnsDigdir()
    {
        ClaimsPrincipal user = PrincipalWithConsumer(ConsumerClaim(DigdirOrgNumber));

        Assert.Equal(
            DecisionTelemetry.DigdirCallerDimensionValue,
            PdpCallerHelper.GetExternalCallerKind(user));
    }

    [Fact]
    public void GetExternalCallerKind_OtherConsumer_ReturnsOwner()
    {
        ClaimsPrincipal user = PrincipalWithConsumer(ConsumerClaim("974761076"));

        Assert.Equal(
            DecisionTelemetry.OwnerCallerDimensionValue,
            PdpCallerHelper.GetExternalCallerKind(user));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData(@"{""authority"":""iso6523-actorid-upis""}")]
    [InlineData(@"{""ID"":""0192:991825827""}")]
    [InlineData(@"{""authority"":""something-else"",""ID"":""0192:991825827""}")]
    [InlineData(@"{""authority"":""iso6523-actorid-upis"",""ID"":""991825827""}")]
    [InlineData(@"{""authority"":""iso6523-actorid-upis"",""ID"":""0192:""}")]
    [InlineData(@"{""authority"":""iso6523-actorid-upis"",""ID"":""0192:991825827:extra""}")]
    [InlineData(@"{""authority"":123,""ID"":""0192:991825827""}")]
    [InlineData(@"{""authority"":""iso6523-actorid-upis"",""ID"":null}")]
    public void GetExternalCallerKind_UnusableConsumerClaim_ReturnsOwner(string consumerClaimValue)
    {
        ClaimsPrincipal user = PrincipalWithConsumer(consumerClaimValue);

        Assert.Equal(
            DecisionTelemetry.OwnerCallerDimensionValue,
            PdpCallerHelper.GetExternalCallerKind(user));
    }

    [Fact]
    public void GetExternalCallerKind_NoConsumerClaim_ReturnsOwner()
    {
        ClaimsPrincipal user = new(new ClaimsIdentity("mock-org"));

        Assert.Equal(
            DecisionTelemetry.OwnerCallerDimensionValue,
            PdpCallerHelper.GetExternalCallerKind(user));
    }

    [Fact]
    public void GetExternalCallerKind_NullPrincipal_ReturnsOwner()
    {
        Assert.Equal(
            DecisionTelemetry.OwnerCallerDimensionValue,
            PdpCallerHelper.GetExternalCallerKind(null));
    }
}
