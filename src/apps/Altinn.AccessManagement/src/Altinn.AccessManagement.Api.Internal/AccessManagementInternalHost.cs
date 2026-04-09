using Altinn.Authorization.Host.Startup;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Altinn.AccessManagement.Api.Internal;

/// <summary>
/// Configures dependencies for Internal API.
/// </summary>
public static partial class AccessManagementInternalHost
{
    private static ILogger Logger { get; } = StartupLoggerFactory.Create(nameof(AccessManagementInternalHost));

    /// <summary>
    /// Adds access management services to the application builder.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <returns>
    /// The modified <see cref="IHostApplicationBuilder"/> with access management services registered.
    /// </returns>
    public static IHostApplicationBuilder AddAccessManagementInternal(this IHostApplicationBuilder builder)
    {
        Log.AddHost(Logger);
        return builder;
    }

    static partial class Log
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "Add access management internal host.")]
        internal static partial void AddHost(ILogger logger);
    }

    public static void ConfigureOpenAPI(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
                Type = SecuritySchemeType.Http,
                Name = "Authorization",
                In = ParameterLocation.Header,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            options.OperationFilter<SecurityRequirementsOperationFilter>();
            options.EnableAnnotations();

            var originalIdSelector = options.SchemaGeneratorOptions.SchemaIdSelector;
            options.SchemaGeneratorOptions.SchemaIdSelector = (Type t) =>
            {
                if (!t.IsNested)
                {
                    var orig = originalIdSelector(t);

                    return orig;
                }

                var chain = new List<string>();
                do
                {
                    chain.Add(originalIdSelector(t));
                    t = t.DeclaringType;
                }
                while (t != null);
                chain.Reverse();
                return string.Join(".", chain);
            };
        });

        builder.Services.AddUrnSwaggerSupport();
    }
}
