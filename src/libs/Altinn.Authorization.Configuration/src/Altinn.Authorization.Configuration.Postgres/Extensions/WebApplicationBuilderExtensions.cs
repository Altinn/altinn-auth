using Altinn.Authorization.Configuration.AppSettings;
using Altinn.Authorization.Configuration.Postgres.Connection;
using Altinn.Authorization.Configuration.Postgres.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Configuration.Postgres.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring PostgreSQL connections 
    /// in an ASP.NET Core application using the <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    public static class WebApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds and configures PostgreSQL connection support to the application, based on the provided options.
        /// Supports both Managed Identity and Username/Password authentication methods.
        /// </summary>
        /// <param name="builder">The <see cref="IHostApplicationBuilder"/> instance to extend.</param>
        /// <param name="configureOptions">An action that configures the <see cref="AltinnPostgresOptions"/> for the PostgreSQL connection.</param>
        /// <returns>The modified <see cref="IHostApplicationBuilder"/> instance.</returns>
        public static IHostApplicationBuilder AddAltinnDefaultPostgresConnection(this IHostApplicationBuilder builder, Action<AltinnPostgresOptions> configureOptions = null)
        {
            return builder.AddAltinnPostgresConnection(options =>
            {
                var altinnAppSettings = new AltinnAppSettings(builder.Configuration);
                options.Database = altinnAppSettings.Postgres.Database;
                options.Host = altinnAppSettings.Postgres.Host;
                if (builder.Environment.IsDevelopment())
                {
                    options.ConfigureUsernameAndPassword(cred =>
                    {
                        cred.Password = altinnAppSettings.Postgres.Password;
                        cred.Username = altinnAppSettings.Postgres.Username;
                    });
                }
                else
                {
                    options.ConfigureManagedIdentity(cred =>
                    {
                        cred.TokenCredentials = altinnAppSettings.EntraId.Identities.PostgresAdmin.TokenCredential;
                    });
                }

                configureOptions?.Invoke(options);
            });
        }

        /// <summary>
        /// Adds and configures PostgreSQL connection support to the application, based on the provided options.
        /// Supports both Managed Identity and Username/Password authentication methods.
        /// </summary>
        /// <param name="builder">The <see cref="IHostApplicationBuilder"/> instance to extend.</param>
        /// <param name="configureOptions">An action that configures the <see cref="AltinnPostgresOptions"/> for the PostgreSQL connection.</param>
        /// <returns>The modified <see cref="IHostApplicationBuilder"/> instance.</returns>
        public static IHostApplicationBuilder AddAltinnPostgresConnection(this IHostApplicationBuilder builder, Action<AltinnPostgresOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IPostgresConnectionPoolFactory>(services =>
            {
                var options = services.GetRequiredService<IOptions<AltinnPostgresOptions>>();
                if (options.Value.UseManagedIdentity)
                {
                    return new ConnectionPoolManagedIdentityFactory(services.GetRequiredService<ILogger<ConnectionPoolManagedIdentityFactory>>(), options);
                }

                return new UsernameAndPasswordConnectionFactory(services.GetRequiredService<ILogger<UsernameAndPasswordConnectionFactory>>(), options);
            });

            return builder;
        }
    }
}
