using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Common.Authentication.Configuration;
using Altinn.Common.PEP.Authorization;
using Altinn.Platform.Events.Formatters;
using Altinn.ResourceRegistry;
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
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

/************************************************************************************
 * Configure services
 ************************************************************************************/

builder.AddDefaultConfiguration();

builder.AddAltinnServiceDefaults("resource-registry");
MapPostgreSqlConfiguration(builder);

// Add services to the container.
ConfigureServices(builder.Services, builder.Configuration);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
});

builder.Services.AddControllers(opts =>
{
    opts.OutputFormatters.Insert(0, new RdfOutputFormatter());
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

var app = builder.Build();

/************************************************************************************
 * Configure application pipeline
 ************************************************************************************/

app.MapDefaultAltinnEndpoints();

Configure(builder.Configuration);

app.Run();

void ConfigureServices(IServiceCollection services, IConfiguration config)
{
    services.AddMemoryCache();
    services.AddSingleton(config);

    services.AddResourceRegistryCoreServices();
    builder.AddResourceRegistryPersistence();
    services.AddSingleton<IResourceRegistry, ResourceRegistryService>();
    services.AddSingleton<IPRP, PRPClient>();
    services.AddSingleton<IAuthorizationHandler, ScopeAccessHandler>();
    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddSingleton<IAccessTokenGenerator, AccessTokenGenerator>();
    services.AddTransient<ISigningCredentialsResolver, SigningCredentialsResolver>();

    services.Configure<PlatformSettings>(config.GetSection("PlatformSettings"));
    services.Configure<ResourceRegistrySettings>(config.GetSection("ResourceRegistrySettings"));
    services.Configure<OidcProviderSettings>(config.GetSection("OidcProviders"));
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

    services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "Standard Authorization header using the Bearer scheme. Example: \"bearer {token}\"",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey
        });
        options.OperationFilter<SecurityRequirementsOperationFilter>();
    });
    services.AddUrnSwaggerSupport();
    services.AddHttpClient<IAccessManagementClient, AccessManagementClient>();
    services.AddHttpClient<IOrgListClient, OrgListClient>();
    services.AddHttpClient<IAltinn2Services, Altinn2ServicesClient>();
    services.AddHttpClient<IApplications, ApplicationsClient>();
    services.AddAltinnRegisterClient();

    services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthzConstants.POLICY_SCOPE_RESOURCEREGISTRY_WRITE, policy => policy
            .RequireScopeAnyOf(AuthzConstants.SCOPE_RESOURCE_ADMIN, AuthzConstants.SCOPE_RESOURCE_WRITE));
        options.AddPolicy(AuthzConstants.POLICY_ACCESS_LIST_READ, policy => policy
            .RequireScopeAnyOf(AuthzConstants.SCOPE_RESOURCE_ADMIN, AuthzConstants.SCOPE_ACCESS_LIST_READ, AuthzConstants.SCOPE_ACCESS_LIST_WRITE)
            .RequireUserOwnsResource());
        options.AddPolicy(AuthzConstants.POLICY_ACCESS_LIST_WRITE, policy => policy
            .RequireScopeAnyOf(AuthzConstants.SCOPE_RESOURCE_ADMIN, AuthzConstants.SCOPE_ACCESS_LIST_WRITE)
            .RequireUserOwnsResource());
        options.AddPolicy(AuthzConstants.POLICY_ADMIN, policy => policy
            .RequireScopeAnyOf(AuthzConstants.SCOPE_RESOURCE_ADMIN));
    });
    services.AddResourceRegistryAuthorizationHandlers();
}

void Configure(IConfiguration config)
{
    Console.WriteLine("Startup // Configure");

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        Console.WriteLine("IsDevelopment || IsStaging");

        app.UseDeveloperExceptionPage();

        // Enable higher level of detail in exceptions related to JWT validation
        IdentityModelEventSource.ShowPII = true;
    }
    else
    {
        app.UseExceptionHandler("/resourceregistry/api/v1/error");
    }

    app.UseMiddleware<RequestForwarderLogMiddleware>("before forwarder middleware");
    app.UseForwardedHeaders();
    app.UseMiddleware<RequestForwarderLogMiddleware>("after forwarder middleware");

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
}

// Note: eventually we can rename the configuration values and remove this mapping
static void MapPostgreSqlConfiguration(IHostApplicationBuilder builder)
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

/// <summary>
/// Startup class.
/// </summary>
public partial class Program 
{
}
