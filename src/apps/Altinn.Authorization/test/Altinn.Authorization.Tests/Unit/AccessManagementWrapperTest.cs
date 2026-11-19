using System.Net;
using System.Text;
using Altinn.Authorization.Api.Contracts.Authorization;
using Altinn.Authorization.Tests.Util;
using Altinn.Authorization.Xacml.Constants;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Services.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.Authorization.Tests.Unit
{
    [UnitTest]
    public class AccessManagementWrapperTest
    {
        private readonly QueuedHandler _handler = new QueuedHandler();
        private readonly AccessManagementWrapper _wrapper;

        public AccessManagementWrapperTest()
        {
            _wrapper = new AccessManagementWrapper(
                Options.Create(new GeneralSettings()),
                new AccessManagementClient(
                    new HttpClient(_handler),
                    Options.Create(new PlatformSettings { ApiAccessManagementEndpoint = "https://platform.example.com/accessmanagement/api/v1/" })),
                Mock.Of<IHttpContextAccessor>(),
                TestSetupUtil.CreateHybridCache());
        }

        /// <summary>
        /// Test case: The delegation changes for one app are asked for twice, then for another app.
        /// Expected: The second read is served from the cache; the other app has its own entry and makes its own call.
        /// </summary>
        [Fact]
        public async Task GetAllDelegationChanges_SameInputTwice_CallsAccessManagementOncePerInput()
        {
            // Arrange
            _handler.Enqueue(HttpStatusCode.OK, "[]");
            _handler.Enqueue(HttpStatusCode.OK, "[]");
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            // Act
            await _wrapper.GetAllDelegationChanges(cancellationToken, ForApp("app1"));
            await _wrapper.GetAllDelegationChanges(cancellationToken, ForApp("app1"));
            await _wrapper.GetAllDelegationChanges(cancellationToken, ForApp("app2"));

            // Assert
            _handler.Requests.Should().HaveCount(2);
        }

        private static Action<DelegationChangeInputDto> ForApp(string app) => input =>
        {
            input.Subject = new(AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute, "20001337");
            input.Party = new(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, "50001337");
            input.Resource =
            [
                new(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute, "ttd"),
                new(AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute, app),
            ];
        };

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
