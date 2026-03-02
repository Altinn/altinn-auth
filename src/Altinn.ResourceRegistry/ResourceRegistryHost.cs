using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Authorization.ServiceDefaults;
using Altinn.Common.AccessToken;
using Altinn.Common.AccessToken.Configuration;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Common.Authentication.Configuration;
using Altinn.Common.PEP.Authorization;
using Altinn.Platform.Events.Formatters;
using Altinn.Register.Authorization;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Clients;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Services;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Altinn.ResourceRegistry.Integration;
using Altinn.ResourceRegistry.Integration.Clients;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Models.ApiDescriptions;
using Altinn.ResourceRegistry.Models.ModelBinding;
using Altinn.ResourceRegistry.Persistence.Configuration;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Altinn.ResourceRegistry;

/// <summary>
/// Configures the resource registry host.
/// </summary>
internal static class ResourceRegistryHost
{
    /// <summary>
    /// Configures the resource registry host.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    public static WebApplication Create(string[] args)
    {
        var builder = AltinnHost.CreateWebApplicationBuilder("resource-registry", args);
        var services = builder.Services;
        var config = builder.Configuration;

        MapPostgreSqlConfiguration(builder);
        services.AddMemoryCache();

        services.Configure<KeyVaultSettings>(config.GetSection("kvSetting"));
        services.AddResourceRegistryCoreServices();
        builder.AddResourceRegistryPersistence();
        services.AddSingleton<IResourceRegistry, ResourceRegistryService>();
        services.AddSingleton<IPRP, PRPClient>();
        services.AddSingleton<IAuthorizationHandler, ScopeAccessHandler>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IAccessTokenGenerator, AccessTokenGenerator>();
        services.AddTransient<ISigningCredentialsResolver, SigningCredentialsResolver>();
        services.AddSingleton<IAuthorizationHandler, AccessTokenHandler>();
        services.AddSingleton<IPublicSigningKeyProvider, PublicSigningKeyProvider>(); 
        services.Configure<PlatformSettings>(config.GetSection("PlatformSettings"));
        services.Configure<ResourceRegistrySettings>(config.GetSection("ResourceRegistrySettings"));
        services.Configure<OidcProviderSettings>(config.GetSection("OidcProviders"));
        services.Configure<ActionTranslationsOptions>(config.GetSection("ActionConfig"));   
        services.Configure<PostgreSQLSettings>(config.GetSection("PostgreSQLSettings"));
        services.Configure<AzureStorageConfiguration>(config.GetSection("AzureStorageConfiguration"));
        services.AddOptions<RegisterClientOptions>()
            .Configure((RegisterClientOptions options, IOptions<PlatformSettings> platformSettings) =>
            {
                if (platformSettings.Value is { ApiRegisterEndpoint: { } registerUri })
                {
                    options.Uri = new(registerUri);
                }
            });

        services.AddAuthentication(JwtCookieDefaults.AuthenticationScheme)
            .AddJwtCookie(JwtCookieDefaults.AuthenticationScheme, options =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    options.RequireHttpsMetadata = false;
                }
            });

        services.AddUrnSwaggerSupport();
        services.AddHttpClient<IAccessManagementClient, AccessManagementClient>();
        services.AddHttpClient<IOrgListClient, OrgListClient>();
        services.AddHttpClient<IAltinn2Services, Altinn2ServicesClient>();
        services.AddHttpClient<IApplications, ApplicationsClient>();
        services.AddAltinnRegisterClient();

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthzConstants.POLICY_SCOPE_RESOURCEREGISTRY_WRITE, policy => policy
                .RequireScopeAnyOf(AuthzConstants.SCOPE_RESOURCE_ADMIN, AuthzConstants.SCOPE_RESOURCE_WRITE))
            .AddPolicy(AuthzConstants.POLICY_ACCESS_LIST_READ, policy => policy
                .RequireScopeAnyOf(
                    AuthzConstants.SCOPE_RESOURCE_ADMIN,
                    AuthzConstants.SCOPE_ACCESS_LIST_READ,
                    AuthzConstants.SCOPE_ACCESS_LIST_WRITE,
                    AuthzConstants.SCOPE_ACCESS_LIST_PDP)
                .RequireUserOwnsResource())
            .AddPolicy(AuthzConstants.POLICY_ACCESS_LIST_WRITE, policy => policy
                .RequireScopeAnyOf(AuthzConstants.SCOPE_RESOURCE_ADMIN, AuthzConstants.SCOPE_ACCESS_LIST_WRITE)
                .RequireUserOwnsResource())
            .AddPolicy(AuthzConstants.POLICY_ADMIN, policy => policy
                .RequireScopeAnyOf(AuthzConstants.SCOPE_RESOURCE_ADMIN))
            .AddPolicy(AuthzConstants.POLICY_PLATFORM_COMPONENT_ONLY, policy =>
                policy.Requirements.Add(new AccessTokenRequirement("platform")))
            .AddPolicy(AuthzConstants.POLICY_INTERNAL_OR_PLATFORM, policy =>
                policy.Requirements.Add(new InternalScopeOrAccessTokenRequirement("platform", "altinn:resourceregistry/resource.admin")))
            .AddPolicy(AuthzConstants.POLICY_STUDIO_DESIGNER, policy => policy.Requirements.Add(new ClaimAccessRequirement("urn:altinn:app", "studio.designer")));

        services.AddResourceRegistryAuthorizationHandlers();

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
        });

        builder.Services.AddControllers()
            .AddMvcOptions(opts =>
            {
                opts.OutputFormatters.Add(new RdfOutputFormatter());
                opts.ModelBinderProviders.InsertSingleton<RequestConditionCollection.ModelBinderProvider>(0);
                opts.ModelBinderProviders.InsertSingleton<AccessListIncludesModelBinder>(0);
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer()
            .TryAddEnumerable(ServiceDescriptor.Singleton<IApiDescriptionProvider, ConditionalApiDescriptionProvider>());

        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("oauth2", new()
            {
                Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
            });
            c.OperationFilter<SecurityRequirementsOperationFilter>();

            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var xmlFile = Path.ChangeExtension(assemblyPath, ".xml");
            if (File.Exists(xmlFile))
            {
                c.IncludeXmlComments(xmlFile);
            }

            c.EnableAnnotations();
            c.SupportNonNullableReferenceTypes();
            c.OperationFilter<ConditionalOperationFilter>();
            c.SchemaFilter<AccessListIncludesSchemaFilter>();

            var originalIdSelector = c.SchemaGeneratorOptions.SchemaIdSelector;
            c.SchemaGeneratorOptions.SchemaIdSelector = (Type t) =>
            {
                if (!t.IsNested)
                {
                    return originalIdSelector(t);
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

        return builder.Build();
    }

    // Note: eventually we can rename the configuration values and remove this mapping
    private static void MapPostgreSqlConfiguration(IHostApplicationBuilder builder)
    {
        var runMigrations = builder.Configuration.GetValue<bool>("PostgreSQLSettings:EnableDBConnection");
        var adminConnectionStringFmt = builder.Configuration.GetValue<string>("PostgreSQLSettings:AdminConnectionString");
        var adminConnectionStringPwd = builder.Configuration.GetValue<string>("PostgreSQLSettings:AuthorizationDbAdminPwd");
        var connectionStringFmt = builder.Configuration.GetValue<string>("PostgreSQLSettings:ConnectionString");
        var connectionStringPwd = builder.Configuration.GetValue<string>("PostgreSQLSettings:AuthorizationDbPwd");

        var adminConnectionString = string.Format(adminConnectionStringFmt, adminConnectionStringPwd);
        var connectionString = string.Format(connectionStringFmt, connectionStringPwd);

        var serviceDescriptor = builder.Services.GetAltinnServiceDescriptor();
        var existing = builder.Configuration.GetValue<string>($"ConnectionStrings:{serviceDescriptor.Name}_db");
        if (!string.IsNullOrEmpty(existing))
        {
            return;
        }

        builder.Configuration.AddInMemoryCollection([
            KeyValuePair.Create($"ConnectionStrings:{serviceDescriptor.Name}_db", connectionString),
            KeyValuePair.Create($"ConnectionStrings:{serviceDescriptor.Name}_db_migrate", adminConnectionString),
            KeyValuePair.Create($"Altinn:Npgsql:{serviceDescriptor.Name}:Migrate:Enabled", runMigrations ? "true" : "false"),
        ]);
    }
}
