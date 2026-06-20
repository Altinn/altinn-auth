using System.Security.Claims;
using Altinn.AccessManagement.Api.Enterprise.Controllers;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Common.PEP.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.AccessManagement.Tests.Unit.Controllers.Enterprise;

/// <summary>
/// Direct unit tests for <see cref="ConsentController"/>.GetConsentEvents (the consent receiver
/// listing). Covers the branches that run before, and instead of, the happy-path response: the
/// unauthorized guard when the token carries no consumer party, the query-validation branches, and
/// propagation of a problem returned by the service. The full-pipeline auth, paging and tie-breaking
/// are covered by the integration tests.
/// </summary>
[UnitTest]
public class ConsentControllerTest
{
    private const string ConsumerClaim = """{"authority":"iso6523-actorid-upis","ID":"0192:991825827"}""";

    private static ConsentController CreateController(IConsent consent, ClaimsPrincipal user, int pageSize = 100)
    {
        var settings = new Mock<IOptionsMonitor<ConsentSettings>>();
        settings.Setup(s => s.CurrentValue).Returns(new ConsentSettings { EventsPageSize = pageSize });

        return new ConsentController(consent, new Mock<IPDP>().Object, settings.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } },
        };
    }

    private static ClaimsPrincipal UserWithConsumer() =>
        new(new ClaimsIdentity(new[] { new Claim("consumer", ConsumerClaim) }, "test"));

    private static ClaimsPrincipal UserWithoutConsumer() => new(new ClaimsIdentity());

    // The validation/problem branches return Altinn's ProblemDetailsActionResult, which exposes no
    // public status. Execute it against a minimal context and read the written response status.
    private static async Task<int> ExecutedStatus(IActionResult result)
    {
        var http = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };
        http.Response.Body = new MemoryStream();
        await result.ExecuteResultAsync(new ActionContext(http, new RouteData(), new ActionDescriptor()));
        return http.Response.StatusCode;
    }

    [Fact]
    public async Task GetConsentEvents_NoConsumerClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(new Mock<IConsent>().Object, UserWithoutConsumer());

        var result = await controller.GetConsentEvents();

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetConsentEvents_InvalidContinuationToken_ReturnsBadRequest()
    {
        var controller = CreateController(new Mock<IConsent>().Object, UserWithConsumer());

        var result = await controller.GetConsentEvents(continuationToken: "!!!not-base64!!!");

        (await ExecutedStatus(result)).Should().Be(400);
    }

    [Fact]
    public async Task GetConsentEvents_InvalidEventType_ReturnsBadRequest()
    {
        var controller = CreateController(new Mock<IConsent>().Object, UserWithConsumer());

        var result = await controller.GetConsentEvents(eventTypes: new[] { "not-a-real-event" });

        (await ExecutedStatus(result)).Should().Be(400);
    }

    [Fact]
    public async Task GetConsentEvents_ServiceReturnsProblem_PropagatesProblemStatus()
    {
        var consent = new Mock<IConsent>();
        consent
            .Setup(s => s.GetConsentEventsForParty(It.IsAny<ConsentPartyUrn>(), It.IsAny<ConsentEventsQuery>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Problems.ConsentNotFound);

        var controller = CreateController(consent.Object, UserWithConsumer());

        var result = await controller.GetConsentEvents();

        (await ExecutedStatus(result)).Should().Be((int)Problems.ConsentNotFound.StatusCode);
    }
}
