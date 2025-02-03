using Altinn.Authorization.Integration.Register.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Altinn.Authorization.Integration.Register.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IHostApplicationBuilder"/> to register Altinn Register services.
    /// </summary>
    public static class WebApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the Altinn Register services to the <see cref="IHostApplicationBuilder"/>.
        /// Configures the <see cref="AltinnRegisterOptions"/> using the provided configuration action.
        /// </summary>
        /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to add the services to.</param>
        /// <param name="configureOptions">A delegate to configure the <see cref="AltinnRegisterOptions"/> instance.</param>
        /// <returns>The updated <see cref="IHostApplicationBuilder"/> instance with the Altinn Register services added.</returns>
        /// <remarks>
        /// This method registers the Altinn Register options, HTTP client, and the <see cref="IAltinnRegister"/> implementation to the dependency injection container.
        /// </remarks>
        public static IHostApplicationBuilder AddAltinnRegister(this IHostApplicationBuilder builder, Action<AltinnRegisterOptions> configureOptions)
        {
            builder.Services.AddOptions<AltinnRegisterOptions>()
                .Validate(opts => opts.Endpoint != null)
                .Configure(configureOptions);

            builder.Services.AddHttpClient(RegisterClient.HttpClientName, (serviceProvider, httpClient) =>
            {
            });

            builder.Services.AddSingleton<IAltinnRegister, RegisterClient>();

            return builder;
        }
    }
}
