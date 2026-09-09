using Altinn.AccessManagement.Api.Enduser.Validation;
using Altinn.Authorization.Host.Startup;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

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
        // The v2 routes carry a v{version:apiVersion} segment, so the constraint has to be
        // registered here as well as in the combined host. Without it the route table cannot
        // be built and swagger generation fails.
        builder.Services.AddApiVersioning(options =>
        {
            // Existing routes carry a literal "v1" segment and no api version attribute,
            // so unspecified requests must keep resolving to 1.0. The url segment reader
            // keeps ?api-version=... query parameters inert on those routes.
            options.DefaultApiVersion = new ApiVersion(1.0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            // Formats the version as "'v'major[.minor][-status]" so group names
            // line up with the swagger document names ("v1", "v2", ...).
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        var applicationName = builder.Environment.ApplicationName;
        builder.Services.AddOptions<SwaggerGenOptions>()
            .Configure((SwaggerGenOptions options, IApiVersionDescriptionProvider provider) =>
            {
                // One swagger document per discovered api version, matching the combined host.
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerDoc(description.GroupName, new OpenApiInfo { Title = applicationName, Version = description.ApiVersion.ToString() });
                }
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            // Endpoints without a version group (minimal endpoints) belong to the v1 document.
            // Without this they land in every document, including v2.
            options.DocInclusionPredicate((documentName, apiDescription) => (apiDescription.GroupName ?? "v1") == documentName);

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
