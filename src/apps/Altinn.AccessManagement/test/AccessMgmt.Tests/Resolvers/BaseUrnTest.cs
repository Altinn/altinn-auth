using Altinn.AccessManagement.Core.Resolvers;

// See: overhaul part-2 step 23
namespace Altinn.AccessManagement.Tests.Resolvers;

/// <summary>
/// Pure-unit tests for <see cref="BaseUrn"/> URN string composition.
/// Pins the lowercase, colon-separated URN hierarchy at each nested
/// level — a regression that produced mixed-case or wrongly-nested
/// URNs (e.g. <c>urn:Altinn:Person:userid</c> instead of
/// <c>urn:altinn:person:userid</c>) would silently break URN matching
/// across the entire access-management surface.
/// </summary>
public class BaseUrnTest
{
    [Fact]
    public void Root_String_IsUrn()
        => BaseUrn.String().Should().Be("urn");

    [Fact]
    public void Altinn_String_IsLowercase()
        => BaseUrn.Altinn.String().Should().Be("urn:altinn");

    // ── Person ────────────────────────────────────────────────────────────────

    [Fact]
    public void Person_String_IsLowercase()
        => BaseUrn.Altinn.Person.String().Should().Be("urn:altinn:person");

    [Theory]
    [InlineData("urn:altinn:person:identifier-no")]
    public void Person_IdentifierNo_ComposesCorrectly(string expected)
        => BaseUrn.Altinn.Person.IdentifierNo.Should().Be(expected);

    [Fact]
    public void Person_LeafProperties_PinAll()
    {
        BaseUrn.Altinn.Person.Uuid.Should().Be("urn:altinn:person:uuid");
        BaseUrn.Altinn.Person.UserId.Should().Be("urn:altinn:person:userid");
        BaseUrn.Altinn.Person.PartyId.Should().Be("urn:altinn:person:partyid");
        BaseUrn.Altinn.Person.Firstname.Should().Be("urn:altinn:person:firstname");
        BaseUrn.Altinn.Person.Shortname.Should().Be("urn:altinn:person:shortname");
        BaseUrn.Altinn.Person.Middlename.Should().Be("urn:altinn:person:middlename");
        BaseUrn.Altinn.Person.Lastname.Should().Be("urn:altinn:person:lastname");
    }

    // ── Organization ──────────────────────────────────────────────────────────

    [Fact]
    public void Organization_String_IsLowercase()
        => BaseUrn.Altinn.Organization.String().Should().Be("urn:altinn:organization");

    [Fact]
    public void Organization_LeafProperties_PinAll()
    {
        BaseUrn.Altinn.Organization.IdentifierNo.Should().Be("urn:altinn:organization:identifier-no");
        BaseUrn.Altinn.Organization.Name.Should().Be("urn:altinn:organization:name");
        BaseUrn.Altinn.Organization.PartyId.Should().Be("urn:altinn:organization:partyid");
        BaseUrn.Altinn.Organization.Uuid.Should().Be("urn:altinn:organization:uuid");
    }

    // ── EnterpriseUser ────────────────────────────────────────────────────────

    [Fact]
    public void EnterpriseUser_String_IsLowercase()
        => BaseUrn.Altinn.EnterpriseUser.String().Should().Be("urn:altinn:enterpriseuser");

    [Fact]
    public void EnterpriseUser_LeafProperties_PinAll()
    {
        BaseUrn.Altinn.EnterpriseUser.Username.Should().Be("urn:altinn:enterpriseuser:username");
        BaseUrn.Altinn.EnterpriseUser.Uuid.Should().Be("urn:altinn:enterpriseuser:uuid");
        BaseUrn.Altinn.EnterpriseUser.UserId.Should().Be("urn:altinn:enterpriseuser:userid");
    }

    [Fact]
    public void EnterpriseUser_NestedOrganization_ComposesAtTwoLevels()
    {
        BaseUrn.Altinn.EnterpriseUser.Organization.String().Should().Be("urn:altinn:enterpriseuser:organization");
        BaseUrn.Altinn.EnterpriseUser.Organization.Uuid.Should().Be("urn:altinn:enterpriseuser:organization:uuid");
    }

    // ── Resource (mixed: ResourceRegistryId/AppOwner/AppId are hardcoded) ────

    [Fact]
    public void Resource_HardcodedConstants_PinAll()
    {
        BaseUrn.Altinn.Resource.ResourceRegistryId.Should().Be("urn:altinn:resource");
        BaseUrn.Altinn.Resource.AppOwner.Should().Be("urn:altinn:org");
        BaseUrn.Altinn.Resource.AppId.Should().Be("urn:altinn:app");
    }

    [Fact]
    public void Resource_DerivedProperties_AreLowercase()
    {
        BaseUrn.Altinn.Resource.String().Should().Be("urn:altinn:resource");
        BaseUrn.Altinn.Resource.Type.Should().Be("urn:altinn:resource:type");
        BaseUrn.Altinn.Resource.Delegable.Should().Be("urn:altinn:resource:delegable");
    }

    // ── InternalIds collection — pin set membership ──────────────────────────

    [Fact]
    public void InternalIds_ContainsExpectedAttributeIdentifiers()
    {
        BaseUrn.InternalIds.Should().HaveCount(5);
        BaseUrn.InternalIds.Should().Contain(BaseUrn.Altinn.Organization.PartyId);
        BaseUrn.InternalIds.Should().Contain(BaseUrn.Altinn.Person.PartyId);
        BaseUrn.InternalIds.Should().Contain(BaseUrn.Altinn.Person.UserId);
        BaseUrn.InternalIds.Should().Contain(BaseUrn.Altinn.EnterpriseUser.UserId);
    }
}
