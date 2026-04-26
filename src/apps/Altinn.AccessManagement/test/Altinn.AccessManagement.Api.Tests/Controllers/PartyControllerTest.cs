using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Altinn.AccessManagement.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.Authorization.Api.Contracts.Party;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Moq;
using ValidationErrors = Altinn.AccessMgmt.Core.Utils.Models.ValidationErrors;

namespace Altinn.AccessManagement.Api.Tests.Controllers;

public class PartyControllerTest
{
    private static PartyController CreateSut(IPartyService svc)
    {
        var controller = new PartyController(svc);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static string MakeToken(string appClaimValue = null)
    {
        var handler = new JwtSecurityTokenHandler();
        var claims = appClaimValue != null
            ? new[] { new Claim(AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, appClaimValue) }
            : Array.Empty<Claim>();

        var token = new JwtSecurityToken(claims: claims);
        return handler.WriteToken(token);
    }

    private static ValidationProblemInstance MakeValidationProblem() =>
        ValidationComposer.Validate(() =>
            (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidPartyUrn, "QUERY/test"));

    private static readonly PartyBaseDto SampleParty = new()
    {
        PartyUuid = Guid.NewGuid(),
        EntityType = "Systembruker",
        EntityVariantType = "Default",
        DisplayName = "Test Party"
    };

    [Fact]
    public async Task AddParty_NullToken_ReturnsUnauthorized()
    {
        var result = await CreateSut(new Mock<IPartyService>().Object).AddParty(SampleParty, null);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddParty_TokenWithoutAppClaim_ReturnsUnauthorized()
    {
        var token = MakeToken(appClaimValue: null);

        var result = await CreateSut(new Mock<IPartyService>().Object).AddParty(SampleParty, token);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddParty_TokenWithWrongApp_ReturnsUnauthorized()
    {
        var token = MakeToken("not-authentication");

        var result = await CreateSut(new Mock<IPartyService>().Object).AddParty(SampleParty, token);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddParty_ValidToken_ServiceProblem_ReturnsProblem()
    {
        var token = MakeToken("authentication");
        var svc = new Mock<IPartyService>();
        svc.Setup(s => s.AddParty(SampleParty, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<AddPartyResultDto>(MakeValidationProblem()));

        var result = await CreateSut(svc.Object).AddParty(SampleParty, token);

        Assert.IsNotType<OkObjectResult>(result.Result);
        Assert.IsNotType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task AddParty_ValidToken_PartyCreated_ReturnsCreated()
    {
        var token = MakeToken("authentication");
        var dto = new AddPartyResultDto { PartyUuid = Guid.NewGuid(), PartyCreated = true };
        var svc = new Mock<IPartyService>();
        svc.Setup(s => s.AddParty(SampleParty, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<AddPartyResultDto>(dto));

        var result = await CreateSut(svc.Object).AddParty(SampleParty, token);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task AddParty_ValidToken_PartyExisting_ReturnsOk()
    {
        var token = MakeToken("authentication");
        var dto = new AddPartyResultDto { PartyUuid = Guid.NewGuid(), PartyCreated = false };
        var svc = new Mock<IPartyService>();
        svc.Setup(s => s.AddParty(SampleParty, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Result<AddPartyResultDto>(dto));

        var result = await CreateSut(svc.Object).AddParty(SampleParty, token);

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
