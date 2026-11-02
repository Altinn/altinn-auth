using System.Net;
using System.Text;
using Altinn.Authorization.Tests.Util;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Services;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Register.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.Authorization.Tests.Unit
{
    [UnitTest]
    public class RegisterServiceTest
    {
        private readonly QueuedHandler _handler = new QueuedHandler();
        private readonly RegisterService _registerService;

        public RegisterServiceTest()
        {
            _registerService = new RegisterService(
                new HttpClient(_handler),
                Mock.Of<IHttpContextAccessor>(a => a.HttpContext == new DefaultHttpContext()),
                Mock.Of<IAccessTokenGenerator>(g => g.GenerateAccessToken("platform", "authorization") == "token"),
                Options.Create(new GeneralSettings { RuntimeCookieName = "AltinnStudioRuntime" }),
                Options.Create(new PlatformSettings { ApiRegisterEndpoint = "https://platform.example.com/register/api/v1/" }),
                NullLogger<IRegisterService>.Instance,
                TestSetupUtil.CreateHybridCache());
        }

        /// <summary>
        /// Test case: Register answers a party list where the party has no uuid, then the same party is asked for again.
        /// Expected: The party is returned and cached under its id without an error, so the second read makes no call.
        /// </summary>
        [Fact]
        public async Task GetPartiesAsync_PartyWithoutUuid_IsCachedByIdAndDoesNotThrow()
        {
            // Arrange
            _handler.Enqueue(HttpStatusCode.OK, """[{"partyId":50001337,"partyTypeName":2,"orgNumber":"910000000","name":"Enhet uten uuid"}]""");

            // Act
            List<Party> first = await _registerService.GetPartiesAsync(new List<int> { 50001337 }, cancellationToken: TestContext.Current.CancellationToken);
            List<Party> second = await _registerService.GetPartiesAsync(new List<int> { 50001337 }, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            first.Should().ContainSingle().Which.PartyUuid.Should().BeNull();
            second.Should().ContainSingle().Which.PartyId.Should().Be(50001337);
            _handler.Requests.Should().HaveCount(1);
        }

        /// <summary>
        /// Answers each request with the next queued response and records the requests it saw.
        /// </summary>
        private sealed class QueuedHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses = new Queue<HttpResponseMessage>();

            public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

            public void Enqueue(HttpStatusCode status, string body)
            {
                _responses.Enqueue(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                return Task.FromResult(_responses.Dequeue());
            }
        }
    }
}
