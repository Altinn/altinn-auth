using Altinn.ResourceRegistry.Core.Models;
using FluentAssertions;
using System;
using Xunit;

namespace Altinn.ResourceRegistry.Tests.Models;

public class HttpDateTimeHeaderValueTests
{
    public static TheoryData<DateTimeOffset, DateTimeOffset> Cases =>
        new()
        {
            { new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.Zero), new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.Zero) },
            { new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.Zero), new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero) },
            { new DateTimeOffset(2022, 02, 02, 02, 02, 02, TimeSpan.Zero), new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.Zero) },
            { new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.FromHours(1)), new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.Zero) },
            { new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.Zero), new DateTimeOffset(2021, 01, 01, 01, 01, 01, TimeSpan.FromHours(1)) },
        };

    [Theory]
    [MemberData(nameof(Cases))]
    public void CheckComparisons(DateTimeOffset left, DateTimeOffset right)
    {
        var leftHttp = new HttpDateTimeHeaderValue(left);
        var rightHttp = new HttpDateTimeHeaderValue(right);

        leftHttp.CompareTo(rightHttp).Should().Be(left.CompareTo(right));
        rightHttp.CompareTo(leftHttp).Should().Be(right.CompareTo(left));
        leftHttp.CompareTo(right).Should().Be(left.CompareTo(right));
        rightHttp.CompareTo(left).Should().Be(right.CompareTo(left));

        (leftHttp == rightHttp).Should().Be(left == right);
        (leftHttp != rightHttp).Should().Be(left != right);
        (leftHttp < rightHttp).Should().Be(left < right);
        (leftHttp > rightHttp).Should().Be(left > right);
        (leftHttp <= rightHttp).Should().Be(left <= right);
        (leftHttp >= rightHttp).Should().Be(left >= right);

        (leftHttp == right).Should().Be(left == right);
        (leftHttp != right).Should().Be(left != right);
        (leftHttp < right).Should().Be(left < right);
        (leftHttp > right).Should().Be(left > right);
        (leftHttp <= right).Should().Be(left <= right);
        (leftHttp >= right).Should().Be(left >= right);

        (left == rightHttp).Should().Be(left == right);
        (left != rightHttp).Should().Be(left != right);
        (left < rightHttp).Should().Be(left < right);
        (left > rightHttp).Should().Be(left > right);
        (left <= rightHttp).Should().Be(left <= right);
        (left >= rightHttp).Should().Be(left >= right);

        (rightHttp == leftHttp).Should().Be(right == left);
        (rightHttp != leftHttp).Should().Be(right != left);
        (rightHttp < leftHttp).Should().Be(right < left);
        (rightHttp > leftHttp).Should().Be(right > left);
        (rightHttp <= leftHttp).Should().Be(right <= left);
        (rightHttp >= leftHttp).Should().Be(right >= left);

        (rightHttp == left).Should().Be(right == left);
        (rightHttp != left).Should().Be(right != left);
        (rightHttp < left).Should().Be(right < left);
        (rightHttp > left).Should().Be(right > left);
        (rightHttp <= left).Should().Be(right <= left);
        (rightHttp >= left).Should().Be(right >= left);

        (right == leftHttp).Should().Be(right == left);
        (right != leftHttp).Should().Be(right != left);
        (right < leftHttp).Should().Be(right < left);
        (right > leftHttp).Should().Be(right > left);
        (right <= leftHttp).Should().Be(right <= left);
        (right >= leftHttp).Should().Be(right >= left);

        if (left == right)
        {
            leftHttp.GetHashCode().Should().Be(right.GetHashCode());
            leftHttp.Equals(rightHttp).Should().BeTrue();
            rightHttp.Equals(leftHttp).Should().BeTrue();
        }
        else
        {
            leftHttp.Equals(rightHttp).Should().BeFalse();
            rightHttp.Equals(leftHttp).Should().BeFalse();
        }
    }
}
