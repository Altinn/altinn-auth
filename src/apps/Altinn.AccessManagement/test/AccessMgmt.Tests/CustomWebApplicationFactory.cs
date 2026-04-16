using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Tests
{
    /// <summary>
    /// CustomWebApplicationFactory for integration tests
    /// </summary>
    /// <typeparam name="TStartup">Entrypoint</typeparam>
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup>
       where TStartup : class
    {
        /// <summary>
        /// ConfigureWebHost for setup of configuration and test services
        /// </summary>
        /// <param name="builder">IWebHostBuilder</param>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var appsettings = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Logging:LogLevel:*"] = "Warning",
            });

            builder.UseConfiguration(appsettings.Build());

            builder.ConfigureTestServices(services =>
            {
                // Replace the IAuditAccessor registration from production code with test version
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuditAccessor));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddScoped<IAuditAccessor>(sp => new AuditAccessor
                {
                    AuditValues = new AuditValues(SystemEntityConstants.StaticDataIngest.Entity.Id)
                });
            });
        }
    }
}
