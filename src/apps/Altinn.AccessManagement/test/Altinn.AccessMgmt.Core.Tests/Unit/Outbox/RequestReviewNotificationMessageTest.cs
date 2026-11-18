using System.Text.Json;
using Altinn.AccessMgmt.Core.Outbox;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Outbox;

/// <summary>
/// The outbox stores <see cref="RequestReviewNotificationMessage"/> as JSON with default
/// <see cref="JsonSerializer"/> options, and a message can wait in the outbox across a deploy.
/// These tests pin the format so that messages written before and after the deploy can both be read.
/// </summary>
[UnitTest]
public class RequestReviewNotificationMessageTest
{
    private static readonly Guid ResourceId = Guid.Parse("01960001-0000-7000-8000-0000000000a1");
    private static readonly Guid PackageId = Guid.Parse("01960001-0000-7000-8000-0000000000b1");

    [Fact]
    public void Deserialize_MessageWrittenBeforeRequestStatusExisted_KeepsIsApprovedAndHasNoStatus()
    {
        var json = $$"""
            {
              "ReviewerId": "01960001-0000-7000-8000-000000000001",
              "RecipientId": "01960001-0000-7000-8000-000000000002",
              "Resources": [ { "Ref": "{{ResourceId}}", "IsApproved": true } ],
              "Packages": [ { "Ref": "{{PackageId}}", "IsApproved": false } ],
              "Updated": 0
            }
            """;

        var message = JsonSerializer.Deserialize<RequestReviewNotificationMessage>(json);

        var resource = message.Resources.Should().ContainSingle().Subject;
        resource.Ref.Should().Be(ResourceId);
        resource.IsApproved.Should().BeTrue();
        resource.RequestStatus.Should().BeNull();

        var package = message.Packages.Should().ContainSingle().Subject;
        package.Ref.Should().Be(PackageId);
        package.IsApproved.Should().BeFalse();
        package.RequestStatus.Should().BeNull();
    }

    [Fact]
    public void Deserialize_MessageWrittenBeforeRequestStatusExisted_GetsLabelsFromIsApproved()
    {
        var json = $$"""
            { "Resources": [ { "Ref": "{{ResourceId}}", "IsApproved": true } ], "Packages": [ { "Ref": "{{PackageId}}", "IsApproved": false } ] }
            """;

        var message = JsonSerializer.Deserialize<RequestReviewNotificationMessage>(json);

        var resource = message.Resources.Single();
        var package = message.Packages.Single();
        RequestReviewedNotificationHandler.GetStatusLabel(resource.RequestStatus, resource.IsApproved).Should().Be("Godkjent.");
        RequestReviewedNotificationHandler.GetStatusLabel(package.RequestStatus, package.IsApproved).Should().Be("Avslått.");
    }

    [Theory]
    [InlineData(RequestStatusV2.Approved)]
    [InlineData(RequestStatusV2.Rejected)]
    [InlineData(RequestStatusV2.PartialApprove)]
    public void SerializeAndDeserialize_KeepsRequestStatus(RequestStatusV2 status)
    {
        var original = new RequestReviewNotificationMessage
        {
            Resources = [new() { Ref = ResourceId, RequestStatus = status, IsApproved = status != RequestStatusV2.Rejected }],
        };

        var roundTripped = JsonSerializer.Deserialize<RequestReviewNotificationMessage>(JsonSerializer.Serialize(original));

        roundTripped.Resources.Single().RequestStatus.Should().Be(status);
    }
}
