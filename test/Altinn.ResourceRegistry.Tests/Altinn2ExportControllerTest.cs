using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Models.Altinn2;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.Tests.Utils;
using Altinn.ResourceRegistry.TestUtils;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Altinn.ResourceRegistry.Tests
{
    public class Altinn2ExportControllerTest(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : WebApplicationTests(dbFixture, webApplicationFixture)
    {
        protected override void ConfigureTestServices(IServiceCollection services)
        {
            services.AddSingleton<IResourceRegistryRepository, RegisterResourceRepositoryMock>();
            base.ConfigureTestServices(services);
        }

        [Fact]
        public async Task Export_Resource()
        {
            HttpClient client = CreateClient();
            string requestUri = "resourceregistry/api/v1/altinn2export/resource?serviceCode=4485&serviceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
        }


        [Fact]
        public async Task Export_Policy()
        {
            HttpClient client = CreateClient();
            string requestUri = "resourceregistry/api/v1/altinn2export/policy?serviceCode=4485&serviceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
        }

        [Fact]
        public async Task DelegationCount()
        {
            HttpClient client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.admin");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/altinn2export/delegationcount?serviceCode=4485&serviceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };
            httpRequestMessage.Headers.Accept.Clear();
            httpRequestMessage.Headers.Accept.ParseAdd("application/json");

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
            DelegationCountOverview? delegationCountOverview = JsonSerializer.Deserialize<DelegationCountOverview>(responseContent, new JsonSerializerOptions() {  PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
            Assert.NotNull(delegationCountOverview);
            Assert.Equal(13336, delegationCountOverview.NumberOfRelations);
            Assert.Equal(13337, delegationCountOverview.NumberOfDelegations);
        }

        /// <summary>
        /// Calls count endopint with wrong id in token
        /// </summary>
        [Fact]
        public async Task DelegationCount_WrongConsumer()
        {
            HttpClient client = CreateClient();
            string token = PrincipalUtil.GetAccessToken("studi2o.designer");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/altinn2export/delegationcount?serviceCode=4485&serviceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Calls count endopint with wrong id in token
        /// </summary>
        [Fact]
        public async Task DelegationCount_WithoutToken()
        {
            HttpClient client = CreateClient();
            string requestUri = "resourceregistry/api/v1/altinn2export/delegationcount?serviceCode=4485&serviceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Calls SetExpired endpoint without token
        /// </summary>
        [Fact]
        public async Task Setserviceeditionexpired_WithoutToken()
        {
            HttpClient client = CreateClient();
            string requestUri = "resourceregistry/api/v1/altinn2export/setserviceeditionexpired?externalServiceCode=4485&externalServiceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Calls SetExpired endpoint without token
        /// </summary>
        [Fact]
        public async Task Setserviceeditionexpired_WithValidToken()
        {
            HttpClient client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.admin");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/altinn2export/setserviceeditionexpired?externalServiceCode=4485&externalServiceEditionCode=2021";

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
            };

            HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }


        /// <summary>
        /// Tries to trigger batch without Altinn Studio token
        /// </summary>
        [Fact]
        public async Task Trigger_Batch_Unauthorized()
        {
            HttpClient client = CreateClient();
            string requestUri = "resourceregistry/api/v1/altinn2export/exportdelegations";

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE()
               {
                ServiceCode = "123",
                ServiceEditionCode = 12314,
                DateTimeForExport = DateTime.Now,
                ResourceId = "nav_tiltakAvtaleOmArbeidstrening"
            };

            using HttpContent content = JsonContent.Create(exportDelegationsRequestBE);
            HttpResponseMessage response = await client.PostAsync(requestUri, content);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Tries to trigger batch without resourceID is set. 
        /// </summary>
        [Fact]
        public async Task Trigger_Batch_MissingResource()
        {
            HttpClient client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.admin");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/altinn2export/exportdelegations";

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE()
            {
                ServiceCode = "123",
                ServiceEditionCode = 12314,
                DateTimeForExport = DateTime.Now,
                ResourceId = string.Empty
            };

            exportDelegationsRequestBE.ResourceId = null;

            using HttpContent content = JsonContent.Create(exportDelegationsRequestBE);
            HttpResponseMessage response = await client.PostAsync(requestUri, content);

            string responseContent = await response.Content.ReadAsStringAsync();


            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("The ResourceId field is required", responseContent);
        }

        /// <summary>
        /// Tries to trigger batch required parameters
        /// </summary>
        [Fact]
        public async Task Trigger_Batch_Ok()
        {
            HttpClient client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.admin");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/altinn2export/exportdelegations";

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE()
            {
                ServiceCode = "4795",
                ServiceEditionCode = 1,
                DateTimeForExport = DateTime.Now,
                ResourceId = "nav_tiltakAvtaleOmArbeidstrening"
            };

            using HttpContent content = JsonContent.Create(exportDelegationsRequestBE);
            HttpResponseMessage response = await client.PostAsync(requestUri, content);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Tries to trigger batch required parameters but org for resource and service does not match
        /// </summary>
        [Fact]
        public async Task Trigger_Batch_NoMatchingORg()
        {
            HttpClient client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("digdir", "991825827", "altinn:resourceregistry/resource.admin");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/altinn2export/exportdelegations";

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE() 
            { 
                ResourceId = "nav_tiltakAvtaleOmArbeidstrening" , 
                DateTimeForExport = DateTime.Now, 
                ServiceCode = "4804", 
                ServiceEditionCode = 170223
            };
            
            using HttpContent content = JsonContent.Create(exportDelegationsRequestBE);
            HttpResponseMessage response = await client.PostAsync(requestUri, content);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Triggers batch with a ttd resource to migrate delegations for an acn Altinn 2 service
        /// </summary>
        [Fact]
        public async Task Trigger_Batch_MigrateAcnServiceForTtd_OK()
        {
            HttpClient client = CreateClient();
            string token = PrincipalUtil.GetOrgToken("ttd", "991825827", "altinn:resourceregistry/resource.admin");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string requestUri = "resourceregistry/api/v1/altinn2export/exportdelegations";

            ExportDelegationsRequestBE exportDelegationsRequestBE = new ExportDelegationsRequestBE() 
            { 
                ResourceId = "altinn_delegation_resource", 
                DateTimeForExport = DateTime.Now, 
                ServiceCode = "3225", 
                ServiceEditionCode = 1596
            };
            
            using HttpContent content = JsonContent.Create(exportDelegationsRequestBE);
            HttpResponseMessage response = await client.PostAsync(requestUri, content);

            string responseContent = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
