using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models.AccessList;
using Altinn.AccessManagement.Core.Services;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Altinn.Authorization.Api.Contracts.ResourceRegistry.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Tests that the delegation check reads <c>Delegable</c> and <c>AccessListMode</c> from the
/// imported resource in the database, not from the Resource Registry.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.CheckResource(Guid, string, CancellationToken)"/> and
    /// <see cref="ConnectionsController.CheckInstance(Guid, string, string, CancellationToken)"/> against
    /// resources whose imported flags differ from the Resource Registry mock data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data (this class has its own database, so the changes do not leak into other tests):
    /// - nav_sykepenger_dialog and app_skd_sirius-skattemelding-v1: <c>Delegable = false</c>
    /// - nav_sykepenger_sykmelding and app_mat_mattilsynet-baker-konditorvare: <c>AccessListMode = Enabled</c>
    /// </para>
    /// <para>
    /// The <see cref="ResourceRegistryClientMock"/> data marks all four as delegable with access lists
    /// disabled, so a denial can only come from the local resource.
    /// </para>
    /// <para>
    /// Mocks:
    /// - <see cref="PolicyRetrievalPointMock"/> for XACML policies
    /// - <see cref="IAccessListsAuthorizationClient"/> answering <see cref="AccessListAuthorizationResult.NotAuthorized"/>
    /// - <see cref="IContextRetrievalService"/> wrapped by <see cref="CallRecorder"/>, to show that the checks never call
    ///   <see cref="IContextRetrievalService.GetResource"/> while the policy is still read through it
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (an organization, so access lists apply)
    /// </para>
    /// </remarks>
    [IntegrationTest]
    public class DelegationCheckLocalResource : IClassFixture<ApiFixture>
    {
        private const string InstanceId = "urn:altinn:instance-id:50083510/a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5f";

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        // The host is built once per fixture from the first constructor's services, so the mock and the
        // recorded calls are shared by every test in this class.
        private static readonly Mock<IAccessListsAuthorizationClient> AccessLists = new();

        private static readonly ConcurrentQueue<string> ContextRetrievalCalls = new();

        static DelegationCheckLocalResource()
        {
            AccessLists
                .Setup(c => c.AuthorizePartyForAccessList(It.IsAny<AccessListAuthorizationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessListAuthorizationResponse { Result = AccessListAuthorizationResult.NotAuthorized });
        }

        public DelegationCheckLocalResource(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                services.RemoveAll<IAccessListsAuthorizationClient>();
                services.AddSingleton(AccessLists.Object);
                services.RemoveAll<IContextRetrievalService>();
                services.AddSingleton(sp => CallRecorder.Wrap<IContextRetrievalService>(
                    ActivatorUtilities.CreateInstance<ContextRetrievalService>(sp), ContextRetrievalCalls));
            });
            Fixture.EnsureSeedOnce<DelegationCheckLocalResource>(db =>
            {
                foreach (var resource in db.Resources.Where(r => r.Id == TestData.NavSykepengerDialog.Id || r.Id == TestData.SiriusSkattemelding.Id))
                {
                    resource.Delegable = false;
                    resource.AccessListMode = ResourceAccessListMode.Disabled;
                }

                foreach (var resource in db.Resources.Where(r => r.Id == TestData.NavSykepengerSykmelding.Id || r.Id == TestData.MattilsynetBakeryService.Id))
                {
                    resource.AccessListMode = ResourceAccessListMode.Enabled;
                }

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient(Guid partyUuid, params string[] scopes)
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, partyUuid.ToString()));
                claims.Add(new Claim("scope", string.Join(" ", scopes)));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        /// <summary>
        /// Checks the calls recorded during the last request: the policy was read for <paramref name="resourceRefId"/>,
        /// and the resource was never looked up.
        /// </summary>
        private static void AssertResourceNotFetchedFromRegistry(string resourceRefId)
        {
            Assert.Contains($"{nameof(IContextRetrievalService.GetResourcePolicyV2)}({resourceRefId})", ContextRetrievalCalls);
            Assert.DoesNotContain(ContextRetrievalCalls, c => c.StartsWith($"{nameof(IContextRetrievalService.GetResource)}("));
        }

        private static void VerifyAccessListAskedFor(string resourceRefId) =>
            AccessLists.Verify(
                c => c.AuthorizePartyForAccessList(
                    It.Is<AccessListAuthorizationRequest>(r => IsDumboAdventuresAndResource(r, resourceRefId)),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce());

        private static void VerifyAccessListNeverAskedFor(string resourceRefId) =>
            AccessLists.Verify(
                c => c.AuthorizePartyForAccessList(
                    It.Is<AccessListAuthorizationRequest>(r => IsResource(r, resourceRefId)),
                    It.IsAny<CancellationToken>()),
                Times.Never());

        private async Task<T> GetOk<T>(string url)
        {
            ContextRetrievalCalls.Clear();
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync(url, TestContext.Current.CancellationToken);

            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

            T result = JsonSerializer.Deserialize<T>(body, JsonOpts);
            Assert.NotNull(result);
            return result;
        }

        /// <summary>
        /// nav_sykepenger_dialog is delegable in the Resource Registry mock but not in the local resource.
        /// Malin has role and package access to all its rights, so the local flag is the only reason to deny them.
        /// </summary>
        [Fact]
        public async Task CheckResource_WhenLocalResourceNotDelegable_Returns200WithAllRightsDeniedAsResourceNotDelegable()
        {
            ResourceCheckDto result = await GetOk<ResourceCheckDto>(
                $"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource={TestData.NavSykepengerDialog.RefId}");

            Assert.NotEmpty(result.Rights);
            Assert.All(result.Rights, r => Assert.False(r.Result, $"Right '{r.Right.Name}' must not be delegable"));
            Assert.All(result.Rights, r => Assert.Contains(DelegationCheckReasonCode.ResourceNotDelegable, r.ReasonCodes));
            Assert.All(result.Rights, r => Assert.DoesNotContain(DelegationCheckReasonCode.AccessListValidationFail, r.ReasonCodes));
            VerifyAccessListNeverAskedFor(TestData.NavSykepengerDialog.RefId);
            AssertResourceNotFetchedFromRegistry(TestData.NavSykepengerDialog.RefId);
        }

        /// <summary>
        /// nav_sykepenger_sykmelding has access lists disabled in the Resource Registry mock but enabled in the
        /// local resource. Dumbo Adventures is not on any access list, so every right is denied.
        /// </summary>
        [Fact]
        public async Task CheckResource_WhenLocalResourceAccessListModeEnabled_Returns200WithAllRightsDeniedAsAccessListValidationFail()
        {
            ResourceCheckDto result = await GetOk<ResourceCheckDto>(
                $"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource={TestData.NavSykepengerSykmelding.RefId}");

            Assert.NotEmpty(result.Rights);
            Assert.All(result.Rights, r => Assert.False(r.Result, $"Right '{r.Right.Name}' must not be delegable"));
            Assert.All(result.Rights, r => Assert.Contains(DelegationCheckReasonCode.AccessListValidationFail, r.ReasonCodes));
            Assert.All(result.Rights, r => Assert.DoesNotContain(DelegationCheckReasonCode.ResourceNotDelegable, r.ReasonCodes));
            VerifyAccessListAskedFor(TestData.NavSykepengerSykmelding.RefId);
        }

        /// <summary>
        /// The instance delegation check reads the same local flag: app_skd_sirius-skattemelding-v1 is not
        /// delegable locally, so every instance right is denied.
        /// </summary>
        [Fact]
        public async Task CheckInstance_WhenLocalResourceNotDelegable_Returns200WithAllRightsDeniedAsResourceNotDelegable()
        {
            InstanceCheckDto result = await GetOk<InstanceCheckDto>(
                $"{Route}/resources/instances/delegationcheck?party={TestData.DumboAdventures.Id}&resource={TestData.SiriusSkattemelding.RefId}&instance={InstanceId}");

            Assert.NotEmpty(result.Rights);
            Assert.All(result.Rights, r => Assert.False(r.Result, $"Right '{r.Right.Name}' must not be delegable"));
            Assert.All(result.Rights, r => Assert.Contains(DelegationCheckReasonCode.ResourceNotDelegable, r.ReasonCodes));
            Assert.All(result.Rights, r => Assert.DoesNotContain(DelegationCheckReasonCode.AccessListValidationFail, r.ReasonCodes));
            VerifyAccessListNeverAskedFor(TestData.SiriusSkattemelding.RefId);
            AssertResourceNotFetchedFromRegistry(TestData.SiriusSkattemelding.RefId);
        }

        /// <summary>
        /// The instance delegation check applies the local access list mode: app_mat_mattilsynet-baker-konditorvare
        /// has access lists enabled locally and Dumbo Adventures is not listed, so every instance right is denied.
        /// </summary>
        [Fact]
        public async Task CheckInstance_WhenLocalResourceAccessListModeEnabled_Returns200WithAllRightsDeniedAsAccessListValidationFail()
        {
            InstanceCheckDto result = await GetOk<InstanceCheckDto>(
                $"{Route}/resources/instances/delegationcheck?party={TestData.DumboAdventures.Id}&resource={TestData.MattilsynetBakeryService.RefId}&instance={InstanceId}");

            Assert.NotEmpty(result.Rights);
            Assert.All(result.Rights, r => Assert.False(r.Result, $"Right '{r.Right.Name}' must not be delegable"));
            Assert.All(result.Rights, r => Assert.Contains(DelegationCheckReasonCode.AccessListValidationFail, r.ReasonCodes));
            VerifyAccessListAskedFor(TestData.MattilsynetBakeryService.RefId);
        }
    }

    /// <summary>
    /// Tests for <see cref="ConnectionsController.CheckResource(Guid, string, CancellationToken)"/> against a
    /// resource with <c>AccessListMode = Enabled</c> locally, where the party is on the access list for some actions.
    /// </summary>
    /// <remarks>
    /// A class of its own, because the access-list client mock is registered once per fixture and this one
    /// answers <see cref="AccessListAuthorizationResult.Authorized"/> for every action except <c>subscribe</c>.
    /// </remarks>
    [IntegrationTest]
    public class DelegationCheckLocalResourceAccessListAuthorized : IClassFixture<ApiFixture>
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        private readonly Mock<IAccessListsAuthorizationClient> _accessLists = new();

        public DelegationCheckLocalResourceAccessListAuthorized(ApiFixture fixture)
        {
            Fixture = fixture;
            _accessLists
                .Setup(c => c.AuthorizePartyForAccessList(It.IsAny<AccessListAuthorizationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessListAuthorizationResponse { Result = AccessListAuthorizationResult.Authorized });
            _accessLists
                .Setup(c => c.AuthorizePartyForAccessList(It.Is<AccessListAuthorizationRequest>(r => ActionName(r) == "subscribe"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessListAuthorizationResponse { Result = AccessListAuthorizationResult.NotAuthorized });

            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                services.RemoveAll<IAccessListsAuthorizationClient>();
                services.AddSingleton(_accessLists.Object);
            });
            Fixture.EnsureSeedOnce<DelegationCheckLocalResourceAccessListAuthorized>(db =>
            {
                db.Resources.Single(r => r.Id == TestData.NavSykepengerDialog.Id).AccessListMode = ResourceAccessListMode.Enabled;
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        /// <summary>
        /// nav_sykepenger_dialog has access lists enabled locally. Dumbo Adventures is on the list for <c>read</c> and
        /// <c>access</c> but not <c>subscribe</c>. Malin has role and package access to every right, so the access-list
        /// answer for each action decides that right alone.
        /// </summary>
        [Fact]
        public async Task CheckResource_WhenLocalResourceAccessListModeEnabledAndPartyAuthorizedForSomeActions_Returns200WithThoseRightsGranted()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestData.MalinEmilie.Id.ToString()));
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource={TestData.NavSykepengerDialog.RefId}",
                TestContext.Current.CancellationToken);

            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(body, JsonOpts);
            Assert.NotNull(result);
            Assert.Equal(["access", "read", "subscribe"], result.Rights.Select(r => r.Right.Action.Value).Order());

            foreach (RightCheckDto right in result.Rights.Where(r => r.Right.Action.Value != "subscribe"))
            {
                Assert.True(right.Result, $"Right '{right.Right.Name}' should be delegable");
                Assert.DoesNotContain(DelegationCheckReasonCode.AccessListValidationFail, right.ReasonCodes);
            }

            RightCheckDto subscribe = Assert.Single(result.Rights, r => r.Right.Action.Value == "subscribe");
            Assert.False(subscribe.Result, $"Right '{subscribe.Right.Name}' must not be delegable");
            Assert.Contains(DelegationCheckReasonCode.AccessListValidationFail, subscribe.ReasonCodes);

            foreach (string action in new[] { "read", "subscribe", "access" })
            {
                _accessLists.Verify(
                    c => c.AuthorizePartyForAccessList(
                        It.Is<AccessListAuthorizationRequest>(r => IsDumboAdventuresAndResource(r, TestData.NavSykepengerDialog.RefId) && ActionName(r) == action),
                        It.IsAny<CancellationToken>()),
                    Times.Once());
            }
        }
    }

    /// <summary>
    /// True when an access-list request asks for Dumbo Adventures (the party, not Malin) and the given resource.
    /// </summary>
    private static bool IsDumboAdventuresAndResource(AccessListAuthorizationRequest request, string resourceRefId) =>
        request.Subject.Value.IsPartyUuid(out Guid partyUuid) && partyUuid == TestData.DumboAdventures.Id
        && IsResource(request, resourceRefId);

    /// <summary>
    /// True when an access-list request asks about the given resource, whichever party it names.
    /// </summary>
    private static bool IsResource(AccessListAuthorizationRequest request, string resourceRefId) =>
        request.Resource.Value.IsResourceId(out var resourceId) && resourceId.ToString() == resourceRefId;

    /// <summary>
    /// The action-id value an access-list request asks about, such as <c>read</c>.
    /// </summary>
    /// <remarks>
    /// Returned exactly as sent, not trimmed to its last segment: the PDP compares this value with the access list's
    /// action filters as it is, so an action sent as a full URN never matches a filter like <c>read</c> (#5084).
    /// </remarks>
    private static string ActionName(AccessListAuthorizationRequest request) =>
        request.Action.Value is { } action && action.IsActionId(out var actionId)
            ? actionId.ToString()
            : null;

    /// <summary>
    /// Forwards every call to the wrapped service and records it as <c>Method(firstArgument)</c>.
    /// </summary>
    public class CallRecorder : DispatchProxy
    {
        private object _inner;
        private ConcurrentQueue<string> _calls;

        /// <summary>
        /// Wraps <paramref name="inner"/> so that each call on the returned <typeparamref name="T"/> is recorded in <paramref name="calls"/>.
        /// </summary>
        public static T Wrap<T>(T inner, ConcurrentQueue<string> calls)
            where T : class
        {
            T proxy = Create<T, CallRecorder>();
            var recorder = (CallRecorder)(object)proxy;
            recorder._inner = inner;
            recorder._calls = calls;
            return proxy;
        }

        /// <inheritdoc/>
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            _calls.Enqueue($"{targetMethod.Name}({(args is { Length: > 0 } ? args[0] : null)})");
            try
            {
                return targetMethod.Invoke(_inner, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
    }
}
