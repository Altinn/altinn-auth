using Altinn.AccessManagement.Utilities;

namespace Altinn.AccessManagement.Tests.Unit.Utilities;

/// <summary>
/// Pure-logic unit tests for <see cref="HashUtil.GetOrderIndependentHashCode{T}"/>.
/// </summary>
[UnitTest]
public class HashUtilTest
{
    [Fact]
    public void GetOrderIndependentHashCode_EmptyCollection_ReturnsZero()
    {
        HashUtil.GetOrderIndependentHashCode(Array.Empty<int>()).Should().Be(0);
    }

    [Fact]
    public void GetOrderIndependentHashCode_OrderIndependent_SameHashForDifferentOrders()
    {
        int h1 = HashUtil.GetOrderIndependentHashCode(new[] { 1, 2, 3 });
        int h2 = HashUtil.GetOrderIndependentHashCode(new[] { 3, 1, 2 });

        h1.Should().Be(h2);
    }

    [Fact]
    public void GetOrderIndependentHashCode_SingleElement_MatchesDefaultHashCode()
    {
        int expected = unchecked(0 + EqualityComparer<int>.Default.GetHashCode(42));

        HashUtil.GetOrderIndependentHashCode(new[] { 42 }).Should().Be(expected);
    }

    [Fact]
    public void GetOrderIndependentHashCode_DifferentElements_ProduceDifferentHashes()
    {
        int h1 = HashUtil.GetOrderIndependentHashCode(new[] { 1 });
        int h2 = HashUtil.GetOrderIndependentHashCode(new[] { 2 });

        h1.Should().NotBe(h2);
    }

    [Fact]
    public void GetOrderIndependentHashCode_StringCollectionDifferentOrders_ReturnsSameHash()
    {
        int h1 = HashUtil.GetOrderIndependentHashCode(new[] { "a", "b" });
        int h2 = HashUtil.GetOrderIndependentHashCode(new[] { "b", "a" });

        h1.Should().Be(h2);
    }
}
