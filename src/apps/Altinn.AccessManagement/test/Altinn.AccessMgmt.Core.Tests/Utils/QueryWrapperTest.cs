using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Utils.Models;

namespace Altinn.AccessMgmt.Core.Tests.Utils;

/// <summary>
/// Pure unit tests for <see cref="QueryWrapper.WrapQueryResponse{T}"/>.
/// </summary>
public class QueryWrapperTest
{
    [Fact]
    public void WrapQueryResponse_NonEmptyCollection_PageInfoMatchesCount()
    {
        var data = new[] { "a", "b", "c" };
        var response = QueryWrapper.WrapQueryResponse(data);

        response.Data.Should().BeEquivalentTo(data);
        response.Page.TotalSize.Should().Be(3);
        response.Page.PageSize.Should().Be(3);
        response.Page.PageNumber.Should().Be(1);
        response.Page.FirstRowOnPage.Should().Be(0);
        response.Page.LastRowOnPage.Should().Be(3);
    }

    [Fact]
    public void WrapQueryResponse_EmptyCollection_AllCountsZero()
    {
        var data = Array.Empty<int>();
        var response = QueryWrapper.WrapQueryResponse(data);

        response.Data.Should().BeEmpty();
        response.Page.TotalSize.Should().Be(0);
        response.Page.PageSize.Should().Be(0);
        response.Page.LastRowOnPage.Should().Be(0);
    }

    [Fact]
    public void WrapQueryResponse_SingleItem_PageInfoReflectsSingleItem()
    {
        var data = new[] { 42 };
        var response = QueryWrapper.WrapQueryResponse(data);

        response.Page.TotalSize.Should().Be(1);
        response.Page.PageSize.Should().Be(1);
        response.Page.LastRowOnPage.Should().Be(1);
    }
}
