using Altinn.AccessManagement.Api.Enduser.Validation;
using Altinn.Authorization.Host.Startup;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Altinn.AccessManagement.Api.Enduser;

/// <summary>
/// Configures dependencies for Enduser API.
/// </summary>
public static partial class AccessManagementEnduserHost
{
    private static ILogger Logger { get; } = StartupLoggerFactory.Create(nameof(AccessManagementEnduserHost));

    /// <summary>
    /// Adds access management services for end users to the application builder.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <returns>
    /// The modified <see cref="IHostApplicationBuilder"/> with access management services registered.
    /// </returns>
    public static IHostApplicationBuilder AddAccessManagementEnduser(this IHostApplicationBuilder builder)
    {
        Log.AddHost(Logger);
        builder.Services.AddTransient<IInputValidation, InputValidation>();
        return builder;
    }

    static partial class Log
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "Add access management enduser host.")]
        internal static partial void AddHost(ILogger logger);
    }

    /// <summary>
    /// Configures OpenAPI (Swagger) services for the specified web application builder.
    /// </summary>    
    /// <param name="builder">The web application builder to configure with OpenAPI and Swagger services. Cannot be null.</param>
    public static void ConfigureOpenAPI(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey
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
