using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Util;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.ProblemDetails;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.PEP.Interfaces;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AccessMgmt.Tests.Controllers.Enterprise
{
    /// <summary>
    /// Tests for maskinporten controller for consent
    /// </summary>
    public class ConsentControllerTestEnterprise(WebApplicationFixture fixture) : IClassFixture<WebApplicationFixture>
    {
        private WebApplicationFactory<Program> Fixture { get; } = fixture.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IPartiesClient, PartiesClientMock>();
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                services.AddSingleton<IAltinnRolesClient, AltinnRolesClientMock>();
                services.AddSingleton<IPDP, PdpPermitMock>();
            });
        });

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>
        /// Test get consent. Expect a consent in response
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateConsentRequest_Valid()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"   
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            StringContent stringContent = new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(url, stringContent);
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(responseContent);
            ConsentRequestDetailsDto consentInfo = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(responseContent, _jsonOptions);
            Assert.Single(consentInfo.ConsentRights);
            Assert.Single(consentInfo.ConsentRights[0].Metadata);
            Assert.Equal($"https://am.ui.localhost/accessmanagement/ui/consent/request?id={requestID}", consentInfo.ViewUri);
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfo.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfo.ValidTo.Second);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfo.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfo.ConsentRights[0].Action[0]);
            Assert.Equal(consentRequest.ConsentRights[0].Metadata["INNTEKTSAAR"], consentInfo.ConsentRights[0].Metadata["INNTEKTSAAR"]);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfo.ConsentRequestEvents[0].PerformedBy);
            Assert.Equal(ConsentRequestStatusType.Created, consentInfo.Status);
        }

        /// <summary>
        /// Test get consent. Expect a consent in response
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateConsentRequestByOrg_Valid()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "991825827", "altinn:consentrequests.org");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            StringContent stringContent = new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(url, stringContent);
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(responseContent);
            ConsentRequestDetailsDto consentInfo = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(responseContent, _jsonOptions);
            Assert.Single(consentInfo.ConsentRights);
            Assert.Single(consentInfo.ConsentRights[0].Metadata);
            Assert.Equal(consentRequest.To, consentInfo.To);
            Assert.Equal(consentRequest.From, consentInfo.From);
            Assert.Equal("urn:altinn:organization:identifier-no:991825827", consentInfo.HandledBy.ToString());
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfo.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfo.ValidTo.Second);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfo.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfo.ConsentRights[0].Action[0]);
            Assert.Equal(consentRequest.ConsentRights[0].Metadata["INNTEKTSAAR"], consentInfo.ConsentRights[0].Metadata["INNTEKTSAAR"]);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("991825827")), consentInfo.ConsentRequestEvents[0].PerformedBy);
            Assert.Equal(ConsentRequestStatusType.Created, consentInfo.Status);
        }

        /// <summary>
        /// Test get consent. Expect a consent in response
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateConsentRequestByOrg_InvalidUrl()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "hvddps://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "991825827", "altinn:consentrequests.org");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            StringContent stringContent = new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(url, stringContent);
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseContent, _jsonOptions);

            Assert.Equal(StdProblemDescriptors.ErrorCodes.ValidationError, problemDetails.ErrorCode);
            Assert.Single(problemDetails.Errors);
            Assert.Equal(ValidationErrors.InvalidRedirectUrl.ErrorCode, problemDetails.Errors.ToList()[0].ErrorCode);
        }

        /// <summary>
        /// Test get consent. Expect a consent in response
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateConsentRequestDuplicatePost_Valid()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(responseContent);
            ConsentRequestDetailsDto consentInfo = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(responseContent, _jsonOptions);
            Assert.Single(consentInfo.ConsentRights);
            Assert.Single(consentInfo.ConsentRights[0].Metadata);
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfo.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfo.ValidTo.Second);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfo.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfo.ConsentRights[0].Action[0]);
            Assert.Equal(consentRequest.ConsentRights[0].Metadata["INNTEKTSAAR"], consentInfo.ConsentRights[0].Metadata["INNTEKTSAAR"]);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfo.ConsentRequestEvents[0].PerformedBy);

            // Post again. Expects 200 ok since everyhing is the same
            HttpResponseMessage response2 = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent2 = await response2.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            Assert.NotNull(responseContent2);
            ConsentRequestDetailsDto consentInfo2 = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(responseContent, _jsonOptions);
            Assert.Single(consentInfo2.ConsentRights);
            Assert.Single(consentInfo2.ConsentRights[0].Metadata);
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfo2.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfo2.ValidTo.Second);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfo2.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfo2.ConsentRights[0].Action[0]);
            Assert.Equal(consentRequest.ConsentRights[0].Metadata["INNTEKTSAAR"], consentInfo2.ConsentRights[0].Metadata["INNTEKTSAAR"]);
            Assert.Single(consentInfo2.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfo2.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfo2.ConsentRequestEvents[0].PerformedBy);
            Assert.Equal(ConsentRequestStatusType.Created, consentInfo2.Status);
        }

        [Fact]
        public async Task CreateConsentRequestDuplicatePost_InvalidDifferentFrom()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(responseContent);
            ConsentRequestDetailsDto consentInfo = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(responseContent, _jsonOptions);
            Assert.Single(consentInfo.ConsentRights);
            Assert.Single(consentInfo.ConsentRights[0].Metadata);
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfo.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfo.ValidTo.Second);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfo.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfo.ConsentRights[0].Action[0]);
            Assert.Equal(consentRequest.ConsentRights[0].Metadata["INNTEKTSAAR"], consentInfo.ConsentRights[0].Metadata["INNTEKTSAAR"]);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfo.ConsentRequestEvents[0].PerformedBy);

            // Post again but changes from. Should cause problem
            consentRequest.From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025181049"));
            HttpResponseMessage response2 = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent2 = await response2.Content.ReadAsStringAsync();

            // TODO This need to ve created
            Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
            Assert.NotNull(responseContent2);
            AltinnMultipleProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnMultipleProblemDetails>(responseContent2, _jsonOptions);

            Assert.Equal(Problems.ConsentWithIdAlreadyExist.ErrorCode, problemDetails.ErrorCode);
        }

        [Fact]
        public async Task CreateConsentRequest_AndCheckStatus_Valid()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            string location = response.Headers.Location.ToString();   
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(responseContent);
            ConsentRequestDetailsDto consentInfo = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(responseContent, _jsonOptions);
            Assert.Single(consentInfo.ConsentRights);
            Assert.Single(consentInfo.ConsentRights[0].Metadata);
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfo.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfo.ValidTo.Second);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfo.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfo.ConsentRights[0].Action[0]);
            Assert.Equal(consentRequest.ConsentRights[0].Metadata["INNTEKTSAAR"], consentInfo.ConsentRights[0].Metadata["INNTEKTSAAR"]);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfo.ConsentRequestEvents[0].PerformedBy);
            Assert.Equal(ConsentRequestStatusType.Created, consentInfo.Status);

            string getUrl = $"/accessmanagement/api/v1/enterprise/consentrequests/{consentInfo.Id}";
            HttpResponseMessage getResponse = await client.GetAsync(location);
            string getResponseConsent = await getResponse.Content.ReadAsStringAsync();

            Assert.NotNull(getResponseConsent);
            ConsentRequestDetailsDto consentInfoFromGet = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(getResponseConsent, _jsonOptions);
            Assert.Single(consentInfoFromGet.ConsentRights);
            Assert.Single(consentInfoFromGet.ConsentRights[0].Metadata);
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfoFromGet.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfoFromGet.ValidTo.Second);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfoFromGet.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfoFromGet.ConsentRights[0].Action[0]);
            Assert.Equal(consentRequest.ConsentRights[0].Metadata["INNTEKTSAAR"], consentInfoFromGet.ConsentRights[0].Metadata["INNTEKTSAAR"]);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfoFromGet.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfoFromGet.ConsentRequestEvents[0].PerformedBy);
            Assert.Equal(ConsentRequestStatusType.Created, consentInfoFromGet.Status);
        }

        [Fact]
        public async Task CreateConsentRequestRequiredDelegator_AndCheckStatus_Valid()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                RequiredDelegator = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            string location = response.Headers.Location.ToString();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(responseContent);
            ConsentRequestDetailsDto consentInfo = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(responseContent, _jsonOptions);
            Assert.Single(consentInfo.ConsentRights);
            Assert.Single(consentInfo.ConsentRights[0].Metadata);
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfo.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfo.ValidTo.Second);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfo.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfo.ConsentRights[0].Action[0]);
            Assert.Equal(consentRequest.ConsentRights[0].Metadata["INNTEKTSAAR"], consentInfo.ConsentRights[0].Metadata["INNTEKTSAAR"]);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfo.ConsentRequestEvents[0].PerformedBy);

            string getUrl = $"/accessmanagement/api/v1/enterprise/consentrequests/{consentInfo.Id}";
            HttpResponseMessage getResponse = await client.GetAsync(location);
            string getResponseConsent = await getResponse.Content.ReadAsStringAsync();

            Assert.NotNull(getResponseConsent);
            ConsentRequestDetailsDto consentInfoFromGet = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(getResponseConsent, _jsonOptions);
            Assert.Single(consentInfoFromGet.ConsentRights);
            Assert.Single(consentInfoFromGet.ConsentRights[0].Metadata);
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfoFromGet.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfoFromGet.ValidTo.Second);
            Assert.Equal(consentRequest.RequiredDelegator, consentInfoFromGet.RequiredDelegator);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfoFromGet.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfoFromGet.ConsentRights[0].Action[0]);
            Assert.Equal(consentRequest.ConsentRights[0].Metadata["INNTEKTSAAR"], consentInfoFromGet.ConsentRights[0].Metadata["INNTEKTSAAR"]);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfoFromGet.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfoFromGet.ConsentRequestEvents[0].PerformedBy);
            Assert.Equal(ConsentRequestStatusType.Created, consentInfoFromGet.Status);
        }

        [Fact]
        public async Task CreateConsentRequestHandledByParty_AndCheckStatus_Valid()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write", "810418192");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            string location = response.Headers.Location.ToString();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(responseContent);
            ConsentRequestDetailsDto consentInfo = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(responseContent, _jsonOptions);
            Assert.Single(consentInfo.ConsentRights);
            Assert.Single(consentInfo.ConsentRights[0].Metadata);
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfo.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfo.ValidTo.Second);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfo.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfo.ConsentRights[0].Action[0]);
            Assert.Equal(consentRequest.ConsentRights[0].Metadata["INNTEKTSAAR"], consentInfo.ConsentRights[0].Metadata["INNTEKTSAAR"]);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfo.ConsentRequestEvents[0].PerformedBy);

            string getUrl = $"/accessmanagement/api/v1/enterprise/consentrequests/{consentInfo.Id}";
            token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.read", "810418192");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage getResponse = await client.GetAsync(location);
            string getResponseConsent = await getResponse.Content.ReadAsStringAsync();

            Assert.NotNull(getResponseConsent);
            ConsentRequestDetailsDto consentInfoFromGet = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(getResponseConsent, _jsonOptions);
            Assert.Single(consentInfoFromGet.ConsentRights);
            Assert.Single(consentInfoFromGet.ConsentRights[0].Metadata);
            Assert.Equal("urn:altinn:organization:identifier-no:810418192", consentInfoFromGet.HandledBy.ToString());
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfoFromGet.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfoFromGet.ValidTo.Second);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfoFromGet.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfoFromGet.ConsentRights[0].Action[0]);
            Assert.Equal(consentRequest.ConsentRights[0].Metadata["INNTEKTSAAR"], consentInfoFromGet.ConsentRights[0].Metadata["INNTEKTSAAR"]);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfoFromGet.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfoFromGet.ConsentRequestEvents[0].PerformedBy);
        }

        /// <summary>
        /// Test get consent. Expect a consent in response
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateConsentRequest_ValidWithoutMetadata()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_navnescore"
                            }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(responseContent);
            ConsentRequestDetailsDto consentInfo = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(responseContent, _jsonOptions);
            Assert.Single(consentInfo.ConsentRights);
            Assert.Null(consentInfo.ConsentRights[0].Metadata);
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfo.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfo.ValidTo.Second);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfo.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfo.ConsentRights[0].Action[0]);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfo.ConsentRequestEvents[0].PerformedBy);
            Assert.Equal(ConsentRequestStatusType.Created, consentInfo.Status);
        }

        /// <summary>
        /// Test get consent. Expect a consent in response
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateConsentRequest_ValidTwin()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "2022" }
                        }
                    },
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_skattegrunnlag"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "fraOgMed", "2018-03" },
                            { "tilOgMed", "2018-06" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestContent = JsonSerializer.Serialize(consentRequest, _jsonOptions);
            HttpResponseMessage response = await client.PostAsync(url, new StringContent(requestContent, Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(responseContent);
            ConsentRequestDetailsDto consentInfo = JsonSerializer.Deserialize<ConsentRequestDetailsDto>(responseContent, _jsonOptions);
            Assert.Equal(2, consentInfo.ConsentRights.Count);
            Assert.Single(consentInfo.ConsentRights[0].Metadata);
            Assert.Equal(consentRequest.ValidTo.Minute, consentInfo.ValidTo.Minute);
            Assert.Equal(consentRequest.ValidTo.Second, consentInfo.ValidTo.Second);
            Assert.Equal(consentRequest.ConsentRights[0].Action.Count, consentInfo.ConsentRights[0].Action.Count);
            Assert.Equal(consentRequest.ConsentRights[0].Action[0], consentInfo.ConsentRights[0].Action[0]);
            Assert.Equal(consentRequest.ConsentRights[0].Metadata["INNTEKTSAAR"], consentInfo.ConsentRights[0].Metadata["INNTEKTSAAR"]);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfo.ConsentRequestEvents[0].PerformedBy);
            Assert.Single(consentInfo.ConsentRequestEvents);
            Assert.Equal(ConsentRequestEventType.Created, consentInfo.ConsentRequestEvents[0].EventType);
            Assert.Equal(ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")), consentInfo.ConsentRequestEvents[0].PerformedBy);
            Assert.Equal(ConsentRequestStatusType.Created, consentInfo.Status);
        }

        /// <summary>
        /// Test get consent. Expect a consent in response
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateConsentRequest_IncompatibleTemplates()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    },
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_skattegrunnlag2"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "fraOgMed", "ADSF" },
                            { "tilOgMed", "ADSF" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseContent, _jsonOptions);

            Assert.Equal("AM-00009", problemDetails.ErrorCode.ToString());
            Assert.Empty(problemDetails.Errors);
        }

        /// <summary>
        /// Scenario: Enterprise tries to add consentrequest with missing metadata for the resource
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateConsentRequest_MissingMetadata()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseContent, _jsonOptions);

            Assert.Equal("AM-00008", problemDetails.ErrorCode.ToString());
        }

        /// <summary>
        /// T
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateConsentRequest_WrongNamingMetadata()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    },
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_skattegrunnlag"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "fraOgMed", "ADSF" },
                            { "tilOgMedwrong", "ADSF" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnMultipleProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnMultipleProblemDetails>(responseContent, _jsonOptions);

            Assert.Equal("STD-00001", problemDetails.ErrorCode.ToString());
            Assert.Equal(2, problemDetails.Problems.Count);
            Assert.Contains(Problems.UnknownConsentMetadata.ErrorCode, problemDetails.Problems.Select(r => r.ErrorCode));
            Assert.Contains(Problems.MissingMetadata.ErrorCode, problemDetails.Problems.Select(r => r.ErrorCode));
        }

        /// <summary>
        /// Try too request consent with meta on a resource that does not require metadata with consent
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateConsentRequest_UnknownMetadata()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_navnescore"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    },
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseContent, _jsonOptions);

            Assert.Equal("AM-00006", problemDetails.ErrorCode.ToString());
            Assert.Equal("Invalid consent metadata", problemDetails.Detail.ToString());
            Assert.Empty(problemDetails.Errors);
            Assert.Equal("inntektsaar", problemDetails.Extensions["key"].ToString());
        }

        /// <summary>
        /// Scenario: Tries to 
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CreateConsentRequest_MissingRights()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseContent, _jsonOptions);

            Assert.Equal(StdProblemDescriptors.ErrorCodes.ValidationError, problemDetails.ErrorCode);
            Assert.Single(problemDetails.Errors);
            Assert.Equal("AM.VLD-00023", problemDetails.Errors.ToList()[0].ErrorCode.ToString());
        }

        [Fact]
        public async Task CreateConsentRequest_MissingAction()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01025161013")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                    new ConsentRightDto
                    {
                        Action = new List<string>(),
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseContent, _jsonOptions);

            Assert.Equal(StdProblemDescriptors.ErrorCodes.ValidationError, problemDetails.ErrorCode);
            Assert.Single(problemDetails.Errors);
            Assert.Equal(ValidationErrors.Required.ErrorCode, problemDetails.Errors.ToList()[0].ErrorCode);
        }

        /// <summary>
        /// Scenario: Enterprise uses valid personidentifer but the person does not exist. It is not in register
        /// Expected: 400 Bad Request after validation in consent controller. There is one validation erorr with given code.
        /// </summary>
        [Fact]
        public async Task CreateConsentRequest_FromIsNonExistingPerson()
        {
            Guid requestID = Guid.CreateVersion7();
            ConsentRequestDto consentRequest = new ConsentRequestDto
            {
                Id = requestID,
                From = ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse("01014922047")),
                To = ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse("810419512")),
                ValidTo = DateTimeOffset.UtcNow.AddDays(1),
                ConsentRights = new List<ConsentRightDto>
                {
                   new ConsentRightDto
                    {
                        Action = new List<string> { "read" },
                        Resource = new List<ConsentResourceAttributeDto>
                        {
                            new ConsentResourceAttributeDto
                            {
                                Type = "urn:altinn:resource",
                                Value = "ttd_inntektsopplysninger"
                            }
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            { "INNTEKTSAAR", "ADSF" }
                        }
                    }
                },
                RequestMessage = new Dictionary<string, string>
                {
                    { "en", "Please approve this consent request" }
                },
                RedirectUrl = "https://www.dnb.no"  
            };

            HttpClient client = GetTestClient();
            string url = $"/accessmanagement/api/v1/enterprise/consentrequests/";

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string token = PrincipalUtil.GetOrgToken(null, "810419512", "altinn:consentrequests.write");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(consentRequest, _jsonOptions), Encoding.UTF8, "application/json"));
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(responseContent);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseContent, _jsonOptions);

            Assert.Equal("AM-00004", problemDetails.ErrorCode.ToString());
            Assert.Empty(problemDetails.Errors);
            Assert.Single(problemDetails.Extensions);
        }

        private HttpClient GetTestClient()
        {
            HttpClient client = Fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
