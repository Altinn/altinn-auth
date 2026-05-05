using Altinn.AccessManagement.Api.Metadata.Controllers;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace AccessMgmt.Tests.Controllers.Metadata;

public class TypesControllerTest
{
    private static TypesController CreateController() => new TypesController();

    // GetOrganizationSubTypes returns the List<SubTypeDto> directly (not wrapped
    // in Ok()), so ActionResult<T>.Value holds the payload while .Result is null.
    [Fact]
    public void GetOrganizationSubTypes_ReturnsNonNullValue()
    {
        var result = CreateController().GetOrganizationSubTypes();
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void GetOrganizationSubTypes_ReturnsNonEmptyList()
    {
        var result = CreateController().GetOrganizationSubTypes();
        Assert.NotEmpty(result.Value);
    }

    [Fact]
    public void GetOrganizationSubTypes_AllItemsHaveNonEmptyName()
    {
        var result = CreateController().GetOrganizationSubTypes();
        Assert.All(result.Value, item => Assert.False(string.IsNullOrWhiteSpace(item.Name)));
    }
}
