using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Altinn.AccessManagement.Api.Internal.Extensions;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Util;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.ProblemDetails;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.PEP.Interfaces;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AccessMgmt.Tests.Controllers.Bff
{
    /// <summary>
    /// Migrated from <c>WebApplicationFixture</c> to <see cref="LegacyApiFixture"/>
    /// in sub-step 16.4b-continued. Because these tests reuse hard-coded
    /// <c>requestId</c> GUIDs and share entity inserts, the class implements
    /// <see cref="IAsyncLifetime"/> and stands up a fresh
    /// <see cref="LegacyApiFixture"/> (hence a fresh per-test EF database,
    /// cloned from the shared EFPostgresFactory template) for every
    /// <c>[Fact]</c> — matching the per-test isolation the legacy
    /// <c>WebApplicationFixture</c> provided via
    /// <c>PostgresServer.NewEFDatabase()</c>.
    /// </summary>
    public class ConsentControllerTestBFF : IAsyncLifetime
    {
        private LegacyApiFixture _fixture = null!;
        private readonly ITestOutputHelper _output;

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.ResourceType ConsentResourceType = new()
        {
            Id = Guid.Parse("0196b0c0-0000-7000-8000-000000000001"),
            Name = "Consent",
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Resource ResourceSkattegrunnlag = new()
        {
            Id = Guid.Parse("0196b0c0-0000-7000-8000-000000000002"),
            Name = "Skattegrunnlag",
            Description = "Consent resource for skattegrunnlag",
            RefId = "ttd_skattegrunnlag",
            ProviderId = ProviderConstants.ResourceRegistry.Id,
            TypeId = ConsentResourceType.Id,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Resource ResourceInntektsopplysninger = new()
        {
            Id = Guid.Parse("0196b0c0-0000-7000-8000-000000000003"),
            Name = "Inntektsopplysninger",
            Description = "Consent resource for inntektsopplysninger",
            RefId = "ttd_inntektsopplysninger",
            ProviderId = ProviderConstants.ResourceRegistry.Id,
            TypeId = ConsentResourceType.Id,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Resource ResourceSkattegrunnlag3 = new()
        {
            Id = Guid.Parse("0196b0c0-0000-7000-8000-000000000004"),
            Name = "Skattegrunnlag3",
            Description = "Consent resource for skattegrunnlag3",
            RefId = "ttd_skattegrunnlag3",
            ProviderId = ProviderConstants.ResourceRegistry.Id,
            TypeId = ConsentResourceType.Id,
        };

        #region Test Entities

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity ElenaFjaerEntity = new()
        {
            Id = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"),
            Name = "ELENA FJÆR",
            RefId = "01025161013",
            PersonIdentifier = "01025161013",
            TypeId = EntityTypeConstants.Person,
            VariantId = EntityVariantConstants.Person,
            PartyId = 513370001,

            // UserId intentionally null: the shared EFPostgresFactory template
            // already seeds TestEntities.PersonOrjan with UserId = 20001337
            // (the same UserId the BFF test tokens claim), so assigning a
            // UserId here would violate the ix_entity_userid unique index.
            // The BFF tests look up ElenaFjær by Id (partyUuid), never by
            // UserId, so leaving it null is safe.
            UserId = null,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity SmekkFullBankEntity = new()
        {
            Id = Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181"),
            Name = "SmekkFull Bank AS",
            RefId = "810419512",
            OrganizationIdentifier = "810419512",
            TypeId = EntityTypeConstants.Organization,
            VariantId = EntityVariantConstants.AS,
            PartyId = 501235,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity DigitaliseringsdirektoratetEntity = new()
        {
            Id = Guid.Parse("cdda2f11-95c5-4be4-9690-54206ff663f6"),
            Name = "DIGITALISERINGSDIREKTORATET",
            RefId = "991825827",
            OrganizationIdentifier = "991825827",
            TypeId = EntityTypeConstants.Organization,
            VariantId = EntityVariantConstants.AS,
            PartyId = 501236,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity KolsaasOgFlaamEntity = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0005-000000004219"),
            Name = "KOLSAAS OG FLAAM",
            RefId = "810418192",
            OrganizationIdentifier = "810418192",
            TypeId = EntityTypeConstants.Organization,
            VariantId = EntityVariantConstants.AS,
            PartyId = 50004219,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity LepsoyOgTonstadEntity = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0005-000000006078"),
            Name = "LEPSØY OG TONSTAD",
            RefId = "910493353",
            OrganizationIdentifier = "910493353",
            TypeId = EntityTypeConstants.Organization,
            VariantId = EntityVariantConstants.AS,
            PartyId = 50006078,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity ConsentToOrgEntity = new()
        {
            Id = Guid.Parse("00000000-0000-0000-0005-000000004646"),
            Name = "CONSENT TO ORG",
            RefId = "310419512",
            OrganizationIdentifier = "310419512",
            TypeId = EntityTypeConstants.Organization,
            VariantId = EntityVariantConstants.AS,
            PartyId = 50004646,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity KariNordmannEntity = new()
        {
            Id = Guid.Parse("d47ac10b-58cc-4372-a567-0e02b2c3d483"),
            Name = "Kari Nordmann",
            RefId = "01025181049",
            PersonIdentifier = "01025181049",
            TypeId = EntityTypeConstants.Person,
            VariantId = EntityVariantConstants.Person,
            PartyId = 501238,
        };

        private static readonly Altinn.AccessMgmt.PersistenceEF.Models.Entity[] ConsentTestEntities =
        [
            ElenaFjaerEntity,
            SmekkFullBankEntity,
            DigitaliseringsdirektoratetEntity,
            KolsaasOgFlaamEntity,
            LepsoyOgTonstadEntity,
            ConsentToOrgEntity,
            KariNordmannEntity,
        ];

        #endregion

        public ConsentControllerTestBFF(ITestOutputHelper output)
        {
            _output = output;
        }

        public async ValueTask InitializeAsync()
        {
            _fixture = new LegacyApiFixture();
            _fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPartiesClient, PartiesClientMock>();
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.RemoveAll<IPublicSigningKeyProvider>();
                services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                services.AddSingleton<IAltinnRolesClient, AltinnRolesClientMock>();
                services.RemoveAll<IPDP>();
                services.AddSingleton<IPDP, PdpPermitMock>();
                services.AddSingleton<IProfileClient, ProfileClientMock>();
                services.AddSingleton<IAltinn2ConsentClient, Altinn2ConsentClientMock>();
            });

            await _fixture.InitializeAsync();
            SeedResources();
        }

        public async ValueTask DisposeAsync()
        {
            await _fixture.DisposeAsync();
        }

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private void SeedResources()
        {
            using IServiceScope scope = _fixture.Services.CreateEFScope(SystemEntityConstants.StaticDataIngest);
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!db.ResourceTypes.AsNoTracking().Any(t => t.Id == ConsentResourceType.Id))
            {
                db.ResourceTypes.Add(ConsentResourceType);
                db.SaveChanges();
            }

            if (!db.Resources.AsNoTracking().Any(r => r.Id == ResourceSkattegrunnlag.Id))
            {
                db.Resources.Add(ResourceSkattegrunnlag);
            }

            if (!db.Resources.AsNoTracking().Any(r => r.Id == ResourceInntektsopplysninger.Id))
            {
                db.Resources.Add(ResourceInntektsopplysninger);
            }

            if (!db.Resources.AsNoTracking().Any(r => r.Id == ResourceSkattegrunnlag3.Id))
            {
                db.Resources.Add(ResourceSkattegrunnlag3);
            }

            foreach (Entity entity in ConsentTestEntities)
            {
                if (!db.Entities.AsNoTracking().Any(e => e.Id == entity.Id))
                {
                    db.Entities.Add(entity);
                }
            }

            db.SaveChanges();

            // Self-referencing PrivatePerson assignment for Elena Fjær
            if (!db.Assignments.AsNoTracking().Any(a => a.FromId == ElenaFjaerEntity.Id && a.ToId == ElenaFjaerEntity.Id && a.RoleId == RoleConstants.PrivatePerson.Id))
            {
                db.Assignments.Add(new Altinn.AccessMgmt.PersistenceEF.Models.Assignment
                {
                    FromId = ElenaFjaerEntity.Id,
                    ToId = ElenaFjaerEntity.Id,
                    RoleId = RoleConstants.PrivatePerson.Id,
                });
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Test case: Get consent request
        /// Scenario: User is authenticated and is the same person that has been request to accept the request
        /// User is authorized for all rights in the consent request
        /// </summary>
        [Fact]
        public async Task GetConsentRequest()
        {
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");

            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/{requestId.ToString()}");
            string responseText = await response.Content.ReadAsStringAsync();
            ConsentRequestDetailsBffDto consentRequest = await response.Content.ReadFromJsonAsync<ConsentRequestDetailsBffDto>();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(requestId, consentRequest.Id);
            Assert.True(consentRequest.ConsentRights.Count > 0);
            Assert.Equal("d5b861c8-8e3b-44cd-9952-5315e5990cf5", consentRequest.From.ValueSpan);
            Assert.Equal("8ef5e5fa-94e1-4869-8635-df86b6219181", consentRequest.To.ValueSpan);  // TODO FIx
            Assert.Equal("https:///www.urlfromsavedreqest.com", consentRequest.RedirectUrl);  // TODO FI
            Assert.Equal("urn:altinn:resource", consentRequest.ConsentRights[0].Resource[0].Type);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentPortalViewMode.Hide, consentRequest.PortalViewMode);
        }

        /// <summary>
        /// Test case: Get consent request
        /// Scenario: User is authenticated and is the same person that has been request to accept the request
        /// User is authorized for all rights in the consent request
        /// </summary>
        [Fact]
        public async Task GetConsentRequestAccessPackage()
        {
            Guid requestId = Guid.Parse("2fe8bd3e-d482-4170-8c09-f44cf31797ce");

            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/{requestId.ToString()}");
            string responseText = await response.Content.ReadAsStringAsync();
            ConsentRequestDetailsBffDto consentRequest = await response.Content.ReadFromJsonAsync<ConsentRequestDetailsBffDto>();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(requestId, consentRequest.Id);
            Assert.True(consentRequest.ConsentRights.Count > 0);
            Assert.Equal("d5b861c8-8e3b-44cd-9952-5315e5990cf5", consentRequest.From.ValueSpan);
            Assert.Equal("8ef5e5fa-94e1-4869-8635-df86b6219181", consentRequest.To.ValueSpan);  // TODO FIx
            Assert.Equal("https:///www.urlfromsavedreqest.com", consentRequest.RedirectUrl);  // TODO FI
            Assert.Equal("urn:altinn:resource", consentRequest.ConsentRights[0].Resource[0].Type);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentPortalViewMode.Hide, consentRequest.PortalViewMode);
        }

        [Fact]
        public async Task GetConsentRequest_Show()
        {
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed46");

            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/{requestId.ToString()}");
            string responseText = await response.Content.ReadAsStringAsync();
            ConsentRequestDetailsBffDto consentRequest = await response.Content.ReadFromJsonAsync<ConsentRequestDetailsBffDto>();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(requestId, consentRequest.Id);
            Assert.True(consentRequest.ConsentRights.Count > 0);
            Assert.Equal("d5b861c8-8e3b-44cd-9952-5315e5990cf5", consentRequest.From.ValueSpan);
            Assert.Equal("8ef5e5fa-94e1-4869-8635-df86b6219181", consentRequest.To.ValueSpan);  // TODO FIx
            Assert.Equal("https:///www.urlfromsavedreqest.com", consentRequest.RedirectUrl);  // TODO FI
            Assert.Equal("urn:altinn:resource", consentRequest.ConsentRights[0].Resource[0].Type);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentPortalViewMode.Show, consentRequest.PortalViewMode);
        }

        /// <summary>
        /// Test case: Get consent request with expired event
        /// Scenario: User is authenticated and is the same person that has been request to accept the request
        /// User is authorized for all rights in the consent request
        /// </summary>
        [Fact]
        public async Task GetConsentRequest_WithExpiredEvent()
        {
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");

            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(-1)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/{requestId.ToString()}");
            ConsentRequestDetailsBffDto consentRequest = await response.Content.ReadFromJsonAsync<ConsentRequestDetailsBffDto>();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Created, consentRequest.ConsentRequestEvents[0].EventType);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Expired, consentRequest.ConsentRequestEvents[1].EventType);
        }

        [Fact]
        public async Task GetConsentRequestWithoutMessagehandledby()
        {
            Guid requestId = Guid.Parse("e579b7a2-7994-4636-9aca-59e114915b70");

            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/{requestId.ToString()}");
            string responseText = await response.Content.ReadAsStringAsync();
            ConsentRequestDetailsBffDto consentRequest = await response.Content.ReadFromJsonAsync<ConsentRequestDetailsBffDto>();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(requestId, consentRequest.Id);
            Assert.True(consentRequest.ConsentRights.Count > 0);
            Assert.Equal("sblanesoknad", consentRequest.TemplateId);
            Assert.Equal(1, consentRequest.TemplateVersion);
            Assert.Equal("d5b861c8-8e3b-44cd-9952-5315e5990cf5", consentRequest.From.ValueSpan);
            Assert.Equal("8ef5e5fa-94e1-4869-8635-df86b6219181", consentRequest.To.ValueSpan);
            Assert.Equal("cdda2f11-95c5-4be4-9690-54206ff663f6", consentRequest.HandledBy.ValueSpan);
            Assert.Null(consentRequest.Requestmessage);
            Assert.Equal("https:///www.urlfromsavedreqest.com", consentRequest.RedirectUrl);
            Assert.Equal("urn:altinn:resource", consentRequest.ConsentRights[0].Resource[0].Type);
        }

        [Fact]
        public async Task AcceptRequest_Valid()
        {
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);

            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };

            // Serialize the object to JSON
            string jsonContent = JsonSerializer.Serialize(consentContextExternal);

            // Create HttpContent from the JSON string
            HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/bff/consentrequests/{requestId.ToString()}/accept/", httpContent);
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            ConsentRequestDetailsBffDto consentInfo = await response.Content.ReadFromJsonAsync<ConsentRequestDetailsBffDto>();
            Assert.Equal(2, consentInfo.ConsentRequestEvents.Count);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), consentInfo.ConsentRequestEvents[0].PerformedBy);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Accepted, consentInfo.ConsentRequestEvents[1].EventType);
        }

        [Fact]
        public async Task AcceptRequest_Valid_AccessPackage()
        {
            Guid requestId = Guid.Parse("2fe8bd3e-d482-4170-8c09-f44cf31797ce");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);

            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };

            // Serialize the object to JSON
            string jsonContent = JsonSerializer.Serialize(consentContextExternal);

            // Create HttpContent from the JSON string
            HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/bff/consentrequests/{requestId.ToString()}/accept/", httpContent);
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            ConsentRequestDetailsBffDto consentInfo = await response.Content.ReadFromJsonAsync<ConsentRequestDetailsBffDto>();
            Assert.Equal(2, consentInfo.ConsentRequestEvents.Count);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), consentInfo.ConsentRequestEvents[0].PerformedBy);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Accepted, consentInfo.ConsentRequestEvents[1].EventType);
        }


        [Fact]
        public async Task AcceptRequest_ValidToExpired()
        {
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(-10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);

            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };

            // Serialize the object to JSON
            string jsonContent = JsonSerializer.Serialize(consentContextExternal);

            // Create HttpContent from the JSON string
            HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/bff/consentrequests/{requestId.ToString()}/accept/", httpContent);
            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseContent, _jsonOptions);
            Assert.Equal(StdProblemDescriptors.ErrorCodes.ValidationError, problemDetails.ErrorCode);
            Assert.Single(problemDetails.Errors);
            Assert.Equal(ValidationErrors.TimeNotInFuture.ErrorCode, problemDetails.Errors.ToList()[0].ErrorCode);
        }

        [Fact]
        public async Task AcceptRequestWithRequiredDelegator_Valid()
        {
            Guid requestId = Guid.Parse("a4253d59-b40f-409a-a3f7-c6395f065192");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);

            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };

            // Serialize the object to JSON
            string jsonContent = JsonSerializer.Serialize(consentContextExternal);

            // Create HttpContent from the JSON string
            HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/bff/consentrequests/{requestId.ToString()}/accept/", httpContent);
            string responseText = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _output.WriteLine($"❌ Request failed with status code: {response.StatusCode}");
                _output.WriteLine("Response content:");
                _output.WriteLine(responseText);
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            ConsentRequestDetailsBffDto consentInfo = await response.Content.ReadFromJsonAsync<ConsentRequestDetailsBffDto>();
            Assert.Equal(2, consentInfo.ConsentRequestEvents.Count);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), consentInfo.ConsentRequestEvents[0].PerformedBy);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Accepted, consentInfo.ConsentRequestEvents[1].EventType);
        }

        [Fact]
        public async Task AcceptRequest_AlreadyRejected()
        {
            Guid performedBy = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5");
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            await repositgo.RejectConsentRequest(requestId, performedBy, default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, performedBy, AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };

            // Serialize the object to JSON
            string jsonContent = JsonSerializer.Serialize(consentContextExternal);

            // Create HttpContent from the JSON string
            HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/bff/consentrequests/{requestId.ToString()}/accept/", httpContent);
            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseContent, _jsonOptions);
            Assert.Equal("AM-00002", problemDetails.ErrorCode.ToString());
        }

        [Fact]
        public async Task RejectRequest_Valid()
        {
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/bff/consentrequests/{requestId.ToString()}/reject/", null);
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ListRequests_One_Valid()
        {
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/list/d5b861c8-8e3b-44cd-9952-5315e5990cf5");
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<ConsentRequestDetailsBffDto> consentRequestList = JsonSerializer.Deserialize<List<ConsentRequestDetailsBffDto>>(responseText, _jsonOptions);
            Assert.Single(consentRequestList);
        }

        [Fact]
        public async Task ListRequests_One_Valid_Hidden_One_Valid_Show()
        {
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            Guid requestIdShow = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed46");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            await repositgo.CreateRequest(await GetRequest(requestIdShow, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/list/d5b861c8-8e3b-44cd-9952-5315e5990cf5");
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<ConsentRequestDetailsBffDto> consentRequestList = JsonSerializer.Deserialize<List<ConsentRequestDetailsBffDto>>(responseText, _jsonOptions);
            Assert.True(consentRequestList.Count == 2);
            Assert.Single(consentRequestList.Where(r => r.Id == requestIdShow).ToList());
            Assert.Single(consentRequestList.Where(r => r.Id == requestIdShow && r.PortalViewMode == Altinn.Authorization.Api.Contracts.Consent.ConsentPortalViewMode.Show).ToList());
            Assert.Single(consentRequestList.Where(r => r.Id == requestId).ToList());
            Assert.Single(consentRequestList.Where(r => r.Id == requestId && r.PortalViewMode == Altinn.Authorization.Api.Contracts.Consent.ConsentPortalViewMode.Hide).ToList());
        }

        [Fact]
        public async Task ListRequests_One_AcceptedAndExpired()
        {
            Guid performedBy = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5");
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(-10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };
            await repositgo.AcceptConsentRequest(requestId, performedBy, consentContextExternal.ToConsentContext());
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/list/d5b861c8-8e3b-44cd-9952-5315e5990cf5");
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<ConsentRequestDetailsBffDto> consentRequestList = JsonSerializer.Deserialize<List<ConsentRequestDetailsBffDto>>(responseText, _jsonOptions);
            Assert.Single(consentRequestList);
            Assert.Contains(consentRequestList[0].ConsentRequestEvents, e => e.EventType == Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Expired);
            Assert.Contains(consentRequestList[0].ConsentRequestEvents, e => e.EventType == Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Accepted);
        }

        [Fact]
        public async Task ListRequests_One_RejectedOneValid()
        {
            Guid performedBy = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5");
            Guid requestId2 = Guid.Parse("e579b7a2-7994-4636-9aca-59e114915b70");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId2, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            await repositgo.RejectConsentRequest(requestId2, performedBy, default);
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            await repositgo.RejectConsentRequest(requestId, performedBy, default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/list/d5b861c8-8e3b-44cd-9952-5315e5990cf5");
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<ConsentRequestDetailsBffDto> consentRequestList = JsonSerializer.Deserialize<List<ConsentRequestDetailsBffDto>>(responseText, _jsonOptions);
            Assert.Equal(2, consentRequestList.Count);
        }

        /// <summary>
        /// Test case: End user rejects a consent request that earlier has been accepted
        /// Expected result: The consent request is rejected and the consent request event is created. Total 3 events
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RejectRequest_AlreadyAccepted()
        {
            Guid performedBy = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5");
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };
            await repositgo.AcceptConsentRequest(requestId, performedBy, consentContextExternal.ToConsentContext());
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/bff/consentrequests/{requestId.ToString()}/reject/", null);
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnMultipleProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnMultipleProblemDetails>(responseContent, _jsonOptions);

            Assert.Equal(Problems.ConsentCantBeRejected.ErrorCode, problemDetails.ErrorCode);
            Assert.Empty(problemDetails.Problems);
        }

        /// <summary>
        /// Test case: End user rejects a consent request that earlier has been accepted
        /// Expected result: The consent request is rejected and the consent request event is created. Total 3 events
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetConsent_Valid()
        {
            Guid performedBy = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5");
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };
            await repositgo.AcceptConsentRequest(requestId, performedBy, consentContextExternal.ToConsentContext());
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consents/{requestId.ToString()}");
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _output.WriteLine($"❌ Request failed with status code: {response.StatusCode}");
                _output.WriteLine("Response content:");
                _output.WriteLine(responseContent);
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Consent consent = await response.Content.ReadFromJsonAsync<Consent>();
            Assert.Equal(requestId, consent.Id);
            Assert.True(consent.ConsentRights.Count > 0);
            Assert.Equal("d5b861c8-8e3b-44cd-9952-5315e5990cf5", consent.From.ValueSpan);
            Assert.Equal("8ef5e5fa-94e1-4869-8635-df86b6219181", consent.To.ValueSpan);  // TODO FIx
            Assert.Equal("urn:altinn:resource", consent.ConsentRights[0].Resource[0].Type);
            Assert.Equal("1", consent.ConsentRights[0].Resource[0].Version);
            Assert.Equal("4", consent.ConsentRights[1].Resource[0].Version);
            Assert.Equal(consentContextExternal.Language, consent.Context.Language);
            Assert.Equal(consent.TemplateId, consent.TemplateId);
            Assert.NotEmpty(consent.ConsentRequestEvents);
            Assert.Equal(2, consent.ConsentRequestEvents.Count);
        }

        /// <summary>
        /// Test case: End user revokes a consent request that earlier has been accepted
        /// Expected result: The consent request is revoked and the consent request event is created. Total 3 events
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RevokeRequest_Valid()
        {
            Guid performedBy = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5");

            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };
            await repositgo.AcceptConsentRequest(requestId, performedBy, consentContextExternal.ToConsentContext());

            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, performedBy, AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/bff/consents/{requestId.ToString()}/revoke/", null);
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            ConsentRequestDetailsBffDto consentInfo = JsonSerializer.Deserialize<ConsentRequestDetailsBffDto>(responseContent, _jsonOptions);
            Assert.Equal(3, consentInfo.ConsentRequestEvents.Count);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), consentInfo.ConsentRequestEvents[0].PerformedBy);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Accepted, consentInfo.ConsentRequestEvents[1].EventType);
            Assert.Equal(Altinn.Authorization.Api.Contracts.Consent.ConsentRequestEventType.Revoked, consentInfo.ConsentRequestEvents[2].EventType);
        }

        [Fact]
        public async Task RevokeRequest_NotAccepted()
        {
            Guid performedBy = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5");

            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);

            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, performedBy, AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/bff/consents/{requestId.ToString()}/revoke/", null);
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnMultipleProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnMultipleProblemDetails>(responseContent, _jsonOptions);

            Assert.Equal(Problems.ConsentCantBeRevoked.ErrorCode, problemDetails.ErrorCode);
        }

        [Fact]
        public async Task GetConsentRequestCount_Created_ReturnsCorrectCount()
        {
            Guid requestId = Guid.Parse("019d7114-413f-7dbd-af65-72caa1c18d04");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/count/d5b861c8-8e3b-44cd-9952-5315e5990cf5?status=Created");
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            int count = JsonSerializer.Deserialize<int>(responseText);
            Assert.True(count >= 1);
        }

        [Fact]
        public async Task GetConsentRequestCount_HiddenPortalMode_ReturnsZero()
        {
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed44");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/count/d5b861c8-8e3b-44cd-9952-5315e5990cf5?status=Created");
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            int count = JsonSerializer.Deserialize<int>(responseText);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task GetConsentRequestCount_Accepted_ReturnsZeroForCreatedOnly()
        {
            Guid requestId = Guid.Parse("019d7110-437e-7560-b91a-5494525b4c66");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);
            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/count/d5b861c8-8e3b-44cd-9952-5315e5990cf5?status=Accepted");
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            int count = JsonSerializer.Deserialize<int>(responseText);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task GetConsentRequestCount_AcceptedStatus_ReturnsCorrectCount()
        {
            Guid performedBy = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5");
            Guid requestId = Guid.Parse("e2071c55-6adf-487b-af05-9198a230ed46");
            IConsentRepository repositgo = _fixture.Services.GetRequiredService<IConsentRepository>();
            await repositgo.CreateRequest(await GetRequest(requestId, DateTimeOffset.Now.AddDays(10)), Altinn.AccessManagement.Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181")), default);

            ConsentContextDto consentContextExternal = new ConsentContextDto
            {
                Language = "nb",
            };
            await repositgo.AcceptConsentRequest(requestId, performedBy, consentContextExternal.ToConsentContext());

            HttpClient client = GetTestClient();
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, performedBy, AuthzConstants.SCOPE_PORTAL_ENDUSER);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/bff/consentrequests/count/d5b861c8-8e3b-44cd-9952-5315e5990cf5?status=Accepted");
            string responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            int count = JsonSerializer.Deserialize<int>(responseText);
            Assert.True(count >= 1);
        }

        private HttpClient GetTestClient()
        {
            HttpClient client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private Task<ConsentRequest> GetRequest(Guid id, DateTimeOffset validTo)
        {
            Stream dataStream = File.OpenRead($"Data/Consent/consent_request_{id.ToString()}.json");
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            ConsentRequest result = JsonSerializer.Deserialize<ConsentRequest>(dataStream, options);
            result.ValidTo = validTo;

            return Task.FromResult(result);
        }
    }
}
