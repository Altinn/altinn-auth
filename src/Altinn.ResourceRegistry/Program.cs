using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Common.Authentication.Configuration;
using Altinn.Common.PEP.Authorization;
using Altinn.Platform.Events.Formatters;
using Altinn.ResourceRegistry;
using Altinn.ResourceRegistry.Configuration;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Clients;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Services;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Altinn.ResourceRegistry.Filters;
using Altinn.ResourceRegistry.Health;
using Altinn.ResourceRegistry.Integration;
using Altinn.ResourceRegistry.Integration.Clients;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Models.ApiDescriptions;
using Altinn.ResourceRegistry.Models.ModelBinding;
using Altinn.ResourceRegistry.Persistence.Configuration;
using AltinnCore.Authentication.JwtCookie;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using Npgsql;
using Swashbuckle.AspNetCore.Filters;
using Yuniql.AspNetCore;
using Yuniql.PostgreSql;
using KeyVaultSettings = Altinn.Common.AccessToken.Configuration.KeyVaultSettings;

ILogger logger;

string applicationInsightsKeySecretName = "ApplicationInsights--InstrumentationKey";

string applicationInsightsConnectionString = string.Empty;

var builder = WebApplication.CreateBuilder(args);
ConfigureSetupLogging();

await SetConfigurationProviders(builder.Configuration);

ConfigureLogging(builder.Logging);

// Add services to the container.
ConfigureServices(builder.Services, builder.Configuration);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
});

builder.Services.AddControllers(opts =>
{
    opts.OutputFormatters.Insert(0, new RdfOutputFormatter());
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

Configure(builder.Configuration);

app.Run();

void ConfigureServices(IServiceCollection services, IConfiguration config)
{
    services.AddControllers(options =>
    {
        options.ModelBinderProviders.InsertSingleton<RequestConditionCollection.ModelBinderProvider>(0);
        options.ModelBinderProviders.InsertSingleton<AccessListIncludesModelBinder>(0);
    })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.WriteIndented = true;
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    services.AddMemoryCache();
    services.AddSingleton(config);
    services.AddHealthChecks().AddCheck<HealthCheck>("resourceregistry_health_check");

    services.AddResourceRegistryCoreServices();
    services.AddResourceRegistryPersistence();
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

    if (!string.IsNullOrEmpty(applicationInsightsConnectionString))
    {
        services.AddSingleton(typeof(ITelemetryChannel), new ServerTelemetryChannel() { StorageFolder = "/tmp/logtelemetry" });
        services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
        {
            ConnectionString = applicationInsightsConnectionString
        });

        services.AddApplicationInsightsTelemetryProcessor<HealthTelemetryFilter>();
        services.AddApplicationInsightsTelemetryProcessor<IdentityTelemetryFilter>();
        services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();

        logger.LogInformation("Startup // ApplicationInsightsConnectionString = {applicationInsightsConnectionString}", applicationInsightsConnectionString);
    }

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
    logger.LogInformation("Startup // Configure");

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        logger.LogInformation("IsDevelopment || IsStaging");

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

    ConfigurePostgreSql();
    
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
    app.MapHealthChecks("/health");
}

async Task SetConfigurationProviders(ConfigurationManager config)
{
    string basePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;

    logger.LogInformation($"Program // Loading Configuration from basePath={basePath}");

    config.SetBasePath(basePath);
    string configJsonFile1 = $"{basePath}/altinn-appsettings/altinn-dbsettings-secret.json";

    logger.LogInformation($"Loading configuration file: '{configJsonFile1}'");
    config.AddJsonFile(configJsonFile1, optional: true, reloadOnChange: true);

    config.AddEnvironmentVariables();
    config.AddCommandLine(args);

    await ConnectToKeyVaultAndSetApplicationInsights(config);
}

void ConfigurePostgreSql()
{
    if (builder.Configuration.GetValue<bool>("PostgreSQLSettings:EnableDBConnection"))
    {
        ConsoleTraceService traceService = new ConsoleTraceService { IsDebugEnabled = true };

        string connectionString = string.Format(
            builder.Configuration.GetValue<string>("PostgreSQLSettings:AdminConnectionString"),
            builder.Configuration.GetValue<string>("PostgreSQLSettings:authorizationDbAdminPwd"));

        string workspacePath = Path.Combine(Environment.CurrentDirectory, builder.Configuration.GetValue<string>("PostgreSQLSettings:WorkspacePath"));
        if (builder.Environment.IsDevelopment())
        {
            workspacePath = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).FullName, builder.Configuration.GetValue<string>("PostgreSQLSettings:WorkspacePath"));
        }

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        var user = connectionStringBuilder.Username;

        app.UseYuniql(
            new PostgreSqlDataService(traceService),
            new PostgreSqlBulkImportService(traceService),
            traceService,
            new Configuration
            {
                Environment = "prod",
                Workspace = workspacePath,
                ConnectionString = connectionString,
                IsAutoCreateDatabase = false,
                IsDebug = true,
                Tokens = [
                    KeyValuePair.Create("YUNIQL-USER", user)
                ]
            });
    }
}

async Task ConnectToKeyVaultAndSetApplicationInsights(ConfigurationManager config)
{
    logger.LogInformation("Program // Connect to key vault and set up application insights");

    KeyVaultSettings keyVaultSettings = new();
    config.GetSection("kvSetting").Bind(keyVaultSettings);

    if (!string.IsNullOrEmpty(keyVaultSettings.ClientId) &&
        !string.IsNullOrEmpty(keyVaultSettings.TenantId) &&
        !string.IsNullOrEmpty(keyVaultSettings.ClientSecret) &&
        !string.IsNullOrEmpty(keyVaultSettings.SecretUri))
    {
        Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", keyVaultSettings.ClientId);
        Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", keyVaultSettings.ClientSecret);
        Environment.SetEnvironmentVariable("AZURE_TENANT_ID", keyVaultSettings.TenantId);

        try
        {
            SecretClient client = new SecretClient(new Uri(keyVaultSettings.SecretUri), new EnvironmentCredential());
            KeyVaultSecret secret = await client.GetSecretAsync(applicationInsightsKeySecretName);
            applicationInsightsConnectionString = string.Format("InstrumentationKey={0}", secret.Value);
        }
        catch (Exception vaultException)
        {
            logger.LogError(vaultException, $"Unable to read application insights key.");
        }

        try
        {
            var azureCredentials = new DefaultAzureCredential();
            config.AddAzureKeyVault(new Uri(keyVaultSettings.SecretUri), azureCredentials);
        }
        catch (Exception vaultException)
        {
            logger.LogError(vaultException, $"Unable to add key vault secrets to config.");
        }
    }
}

void ConfigureSetupLogging()
{
    // Setup logging for the web host creation
    var logFactory = LoggerFactory.Create(builder =>
    {
        builder
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("Altinn.ResourceRegistryService.Program", LogLevel.Debug)
            .AddConsole();
    });

    logger = logFactory.CreateLogger<Program>();

    NpgsqlLoggingConfiguration.InitializeLogging(logFactory);
}

void ConfigureLogging(ILoggingBuilder logging)
{
    // Clear log providers
    logging.ClearProviders();

    // Setup up application insight if ApplicationInsightsConnectionString is available
    if (!string.IsNullOrEmpty(applicationInsightsConnectionString))
    {
        // Add application insights https://docs.microsoft.com/en-us/azure/azure-monitor/app/ilogger
        logging.AddApplicationInsights(
             configureTelemetryConfiguration: (config) => config.ConnectionString = applicationInsightsConnectionString,
             configureApplicationInsightsLoggerOptions: (options) => { });

        // Optional: Apply filters to control what logs are sent to Application Insights.
        // The following configures LogLevel Information or above to be sent to
        // Application Insights for all categories.
        logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Warning);

        // Adding the filter below to ensure logs of all severity from Program.cs
        // is sent to ApplicationInsights.
        logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>(typeof(Program).FullName, LogLevel.Trace);

        // Include request info logs in Application Insights
        logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>(typeof(RequestForwarderLogMiddleware).FullName, LogLevel.Information);
        logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>(typeof(ForwardedHeadersMiddleware).FullName, LogLevel.Trace);
    }
    else
    {
        // If not application insight is available log to console
        logging.AddFilter("Microsoft", LogLevel.Warning);
        logging.AddFilter("System", LogLevel.Warning);
        logging.AddConsole();
    }
}

/// <summary>
/// Startup class.
/// </summary>
public partial class Program 
{
}
