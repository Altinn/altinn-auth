using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Altinn.ResourceRegistry.Tests
{
   
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup>
        where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.test.json")
                    .Build());
            });
        }
    }
    
}
