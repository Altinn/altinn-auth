using System.Net;
using System.Security.Claims;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Api.Enduser.Utils;
using Altinn.AccessManagement.Core;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models.Profile;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;
using Altinn.Platform.Register.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Utils;

/// <summary>
/// Direct unit tests for <see cref="ToUuidResolver"/>.
///
/// <see cref="ToUuidResolver"/> is only reached at runtime through
/// <c>ConnectionsController.AddAssignmentPerson</c> /
/// <c>CheckResourcePerson</c>; these tests cover every branch of the two
/// public-to-friend methods without spinning up a full controller/PDP stack.
/// The class is <see langword="internal"/> and accessible via
/// <c>InternalsVisibleTo</c> on the Api.Enduser project.
/// </summary>
public class ToUuidResolverTest
{
    private static readonly Guid PersonTypeId = EntityTypeConstants.Person.Id;
    private static readonly Guid OrgTypeId = EntityTypeConstants.Organization.Id;

    private readonly Mock<IEntityService> _entityService = new(MockBehavior.Strict);
    private readonly Mock<IUserProfileLookupService> _userProfileLookupService = new(MockBehavior.Strict);

    private ToUuidResolver Sut => new(_entityService.Object, _userProfileLookupService.Object);

    private static HttpContext HttpContextWithUserId(int userId)
    {
        var ctx = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(AltinnCoreClaimTypes.UserId, userId.ToString()),
        });
        ctx.User = new ClaimsPrincipal(identity);
        return ctx;
    }

    // ---- ResolveWithConnectionInputAsync --------------------------------
    [Fact]
    public async Task ResolveWithConnectionInput_NonPersonEntity_ReturnsSuccessWithPassedUuid()
    {
        var toUuid = Guid.NewGuid();
        _entityService
            .Setup(s => s.GetEntity(toUuid, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new Entity { Id = toUuid, TypeId = OrgTypeId }));

        var result = await Sut.ResolveWithConnectionInputAsync(toUuid, allowConnectionInputForPersonEntity: false, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ToUuid.Should().Be(toUuid);
        result.ErrorResult.Should().BeNull();
    }

    [Fact]
    public async Task ResolveWithConnectionInput_EntityNotFound_ReturnsPartyNotFound()
    {
        var toUuid = Guid.NewGuid();
        _entityService
            .Setup(s => s.GetEntity(toUuid, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Entity>(null!));

        var result = await Sut.ResolveWithConnectionInputAsync(toUuid, allowConnectionInputForPersonEntity: false, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ToUuid.Should().Be(Guid.Empty);
        result.ErrorResult.Should().NotBeNull();
    }

    [Fact]
    public async Task ResolveWithConnectionInput_PersonEntity_NotAllowed_ReturnsPersonInputRequired()
    {
        var toUuid = Guid.NewGuid();
        _entityService
            .Setup(s => s.GetEntity(toUuid, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new Entity { Id = toUuid, TypeId = PersonTypeId }));

        var result = await Sut.ResolveWithConnectionInputAsync(toUuid, allowConnectionInputForPersonEntity: false, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ToUuid.Should().Be(Guid.Empty);
        result.ErrorResult.Should().NotBeNull();
    }

    [Fact]
    public async Task ResolveWithConnectionInput_PersonEntity_AllowedForPerson_ReturnsSuccess()
    {
        var toUuid = Guid.NewGuid();
        _entityService
            .Setup(s => s.GetEntity(toUuid, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new Entity { Id = toUuid, TypeId = PersonTypeId }));

        var result = await Sut.ResolveWithConnectionInputAsync(toUuid, allowConnectionInputForPersonEntity: true, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ToUuid.Should().Be(toUuid);
        result.ErrorResult.Should().BeNull();
    }

    [Fact]
    public async Task ResolveWithPersonInput_Ssn_Found_ReturnsUserUuid()
    {
        var userUuid = Guid.NewGuid();
        var person = new PersonInput { PersonIdentifier = "12345678901", LastName = "Nordmann" };
        var ctx = HttpContextWithUserId(42);

        _userProfileLookupService
            .Setup(s => s.GetUserProfile(
                42,
                It.Is<UserProfileLookup>(l => l.Ssn == "12345678901" && l.Username == null),
                "Nordmann"))
            .ReturnsAsync(new NewUserProfile { UserUuid = userUuid });

        var result = await Sut.ResolveWithPersonInputAsync(person, ctx, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ToUuid.Should().Be(userUuid);
        result.ErrorResult.Should().BeNull();
    }

    [Fact]
    public async Task ResolveWithPersonInput_Username_Found_ReturnsUserUuid()
    {
        // 10 digits — not treated as SSN (SSN path requires exactly 11 digits).
        var userUuid = Guid.NewGuid();
        var person = new PersonInput { PersonIdentifier = "someuser", LastName = "Nordmann" };
        var ctx = HttpContextWithUserId(7);

        _userProfileLookupService
            .Setup(s => s.GetUserProfile(
                7,
                It.Is<UserProfileLookup>(l => l.Username == "someuser" && l.Ssn == null),
                "Nordmann"))
            .ReturnsAsync(new NewUserProfile { UserUuid = userUuid });

        var result = await Sut.ResolveWithPersonInputAsync(person, ctx, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ToUuid.Should().Be(userUuid);
    }

    [Fact]
    public async Task ResolveWithPersonInput_Identifier11DigitsNotAllDigits_TreatedAsUsername()
    {
        // 11 chars but contains non-digits → username path.
        var userUuid = Guid.NewGuid();
        var person = new PersonInput { PersonIdentifier = "1234567890a", LastName = "X" };
        var ctx = HttpContextWithUserId(0);

        _userProfileLookupService
            .Setup(s => s.GetUserProfile(
                0,
                It.Is<UserProfileLookup>(l => l.Username == "1234567890a" && l.Ssn == null),
                "X"))
            .ReturnsAsync(new NewUserProfile { UserUuid = userUuid });

        var result = await Sut.ResolveWithPersonInputAsync(person, ctx, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ToUuid.Should().Be(userUuid);
    }

    [Fact]
    public async Task ResolveWithPersonInput_UserUuidEmpty_FallsBackToPartyUuid()
    {
        var partyUuid = Guid.NewGuid();
        var person = new PersonInput { PersonIdentifier = "12345678901", LastName = "Nordmann" };
        var ctx = HttpContextWithUserId(1);

        _userProfileLookupService
            .Setup(s => s.GetUserProfile(1, It.IsAny<UserProfileLookup>(), "Nordmann"))
            .ReturnsAsync(new NewUserProfile
            {
                UserUuid = Guid.Empty,
                Party = new Party { PartyUuid = partyUuid },
            });

        var result = await Sut.ResolveWithPersonInputAsync(person, ctx, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ToUuid.Should().Be(partyUuid);
    }

    [Fact]
    public async Task ResolveWithPersonInput_ProfileNull_ReturnsValidationError()
    {
        var person = new PersonInput { PersonIdentifier = "12345678901", LastName = "Nordmann" };
        var ctx = HttpContextWithUserId(1);

        _userProfileLookupService
            .Setup(s => s.GetUserProfile(1, It.IsAny<UserProfileLookup>(), "Nordmann"))
            .ReturnsAsync((NewUserProfile)null);

        var result = await Sut.ResolveWithPersonInputAsync(person, ctx, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ToUuid.Should().Be(Guid.Empty);
        result.ErrorResult.Should().NotBeNull();
    }

    [Fact]
    public async Task ResolveWithPersonInput_ProfileHasNoUuid_ReturnsPartyNotFound()
    {
        var person = new PersonInput { PersonIdentifier = "12345678901", LastName = "Nordmann" };
        var ctx = HttpContextWithUserId(1);

        _userProfileLookupService
            .Setup(s => s.GetUserProfile(1, It.IsAny<UserProfileLookup>(), "Nordmann"))
            .ReturnsAsync(new NewUserProfile
            {
                UserUuid = Guid.Empty,
                Party = null,
            });

        var result = await Sut.ResolveWithPersonInputAsync(person, ctx, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ToUuid.Should().Be(Guid.Empty);
        result.ErrorResult.Should().NotBeNull();
    }

    [Fact]
    public async Task ResolveWithPersonInput_ProfilePartyUuidEmpty_ReturnsPartyNotFound()
    {
        var person = new PersonInput { PersonIdentifier = "12345678901", LastName = "Nordmann" };
        var ctx = HttpContextWithUserId(1);

        _userProfileLookupService
            .Setup(s => s.GetUserProfile(1, It.IsAny<UserProfileLookup>(), "Nordmann"))
            .ReturnsAsync(new NewUserProfile
            {
                UserUuid = Guid.Empty,
                Party = new Party { PartyUuid = Guid.Empty },
            });

        var result = await Sut.ResolveWithPersonInputAsync(person, ctx, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ToUuid.Should().Be(Guid.Empty);
        result.ErrorResult.Should().NotBeNull();
    }

    [Fact]
    public async Task ResolveWithPersonInput_TooManyFailedLookups_ReturnsTooManyRequests()
    {
        var person = new PersonInput { PersonIdentifier = "12345678901", LastName = "Nordmann" };
        var ctx = HttpContextWithUserId(1);

        _userProfileLookupService
            .Setup(s => s.GetUserProfile(1, It.IsAny<UserProfileLookup>(), "Nordmann"))
            .ThrowsAsync(new TooManyFailedLookupsException());

        var result = await Sut.ResolveWithPersonInputAsync(person, ctx, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ToUuid.Should().Be(Guid.Empty);
        result.ErrorResult.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ResolveWithPersonInput_MissingUserIdClaim_PassesZero()
    {
        // No claims → AuthenticationHelper.GetUserId returns 0.
        var userUuid = Guid.NewGuid();
        var person = new PersonInput { PersonIdentifier = "12345678901", LastName = "Nordmann" };
        var ctx = new DefaultHttpContext();

        _userProfileLookupService
            .Setup(s => s.GetUserProfile(0, It.IsAny<UserProfileLookup>(), "Nordmann"))
            .ReturnsAsync(new NewUserProfile { UserUuid = userUuid });

        var result = await Sut.ResolveWithPersonInputAsync(person, ctx, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ToUuid.Should().Be(userUuid);
    }
}
