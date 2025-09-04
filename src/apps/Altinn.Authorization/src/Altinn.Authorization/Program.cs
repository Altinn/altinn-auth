using System.Reflection;
using Altinn.ApiClients.Maskinporten.Extensions;
using Altinn.ApiClients.Maskinporten.Services;
using Altinn.Authorization.Services.Implementation;
using Altinn.Common.AccessToken;
using Altinn.Common.AccessToken.Configuration;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Common.PEP.Authorization;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Constants;
using Altinn.Platform.Authorization.Extensions;
using Altinn.Platform.Authorization.Filters;
using Altinn.Platform.Authorization.Health;
using Altinn.Platform.Authorization.ModelBinding;
using Altinn.Platform.Authorization.Repositories;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Authorization.Services;
using Altinn.Platform.Authorization.Services.Implementation;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Telemetry;
using AltinnCore.Authentication.JwtCookie;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Swashbuckle.AspNetCore.Filters;
using Yuniql.AspNetCore;
using Yuniql.PostgreSql;

ILogger logger;

string applicationInsightsKeySecretName = "ApplicationInsights--InstrumentationKey";

string applicationInsightsConnectionString = string.Empty;

var builder = WebApplication.CreateBuilder(args);

ConfigureSetupLogging();

await SetConfigurationProviders(builder.Configuration, builder.Environment.IsDevelopment());

ConfigureLogging(builder.Logging);

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

Configure();

app.Run();

void ConfigureSetupLogging()
{
    // Setup logging for the web host creation
    var logFactory = LoggerFactory.Create(builder =>
    {
        builder
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("Altinn.Platform.Authorization.Program", LogLevel.Debug)
            .AddConsole();
    });

    NpgsqlLoggingConfiguration.InitializeLogging(logFactory);
    logger = logFactory.CreateLogger<Program>();
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
    }
    else
    {
        // If not application insight is available log to console
        logging.AddFilter("Microsoft", LogLevel.Warning);
        logging.AddFilter("System", LogLevel.Warning);
        logging.AddConsole();
    }
}

async Task SetConfigurationProviders(ConfigurationManager config, bool isDevelopment)
{
    string basePath = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;

    logger.LogInformation($"Program // Loading Configuration from basePath={basePath}");

    config.SetBasePath(basePath);
    string configJsonFile1 = $"{basePath}/altinn-appsettings/altinn-dbsettings-secret.json";
    string configJsonFile2 = $"{Directory.GetCurrentDirectory()}/appsettings.json";

    if (basePath == "/")
    {
        configJsonFile2 = "/app/appsettings.json";
    }

    logger.LogInformation($"Loading configuration file: '{configJsonFile1}'");
    config.AddJsonFile(configJsonFile1, optional: true, reloadOnChange: true);

    logger.LogInformation($"Loading configuration file2: '{configJsonFile2}'");
    config.AddJsonFile(configJsonFile2, optional: false, reloadOnChange: true);

    config.AddEnvironmentVariables();
    config.AddCommandLine(args);

    await ConnectToKeyVaultAndSetApplicationInsights(config);

    if (isDevelopment)
    {
        config.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
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
            config.AddAzureKeyVault(
                 keyVaultSettings.SecretUri, keyVaultSettings.ClientId, keyVaultSettings.ClientSecret);
        }
        catch (Exception vaultException)
        {
            logger.LogError(vaultException, $"Unable to add key vault secrets to config.");
        }
    }
}

void ConfigureServices(IServiceCollection services, IConfiguration config)
{
    logger.LogInformation("Startup // ConfigureServices");
    services.AddAutoMapper(typeof(Program));
    services.AddControllers().AddXmlSerializerFormatters();
    services.AddHealthChecks().AddCheck<HealthCheck>("authorization_health_check");
    services.AddSingleton(config);
    services.AddSingleton<IParties, PartiesWrapper>();
    services.AddSingleton<IProfile, ProfileWrapper>();
    services.AddSingleton<IRoles, RolesWrapper>();
    services.AddSingleton<IOedRoleAssignmentWrapper, OedRoleAssignmentWrapper>();
    services.AddSingleton<IContextHandler, ContextHandler>();
    services.AddSingleton<IDelegationContextHandler, DelegationContextHandler>();
    services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPoint>();
    services.AddSingleton<IPolicyInformationPoint, PolicyInformationPoint>();
    services.AddSingleton<IPolicyAdministrationPoint, PolicyAdministrationPoint>();
    services.AddSingleton<IPolicyRepository, PolicyRepository>();
    services.AddSingleton<IResourceRegistry, ResourceRegistryWrapper>();
    services.AddSingleton<IInstanceMetadataRepository, InstanceMetadataRepository>();
    services.AddSingleton<IDelegationMetadataRepository, DelegationMetadataRepository>();
    services.AddSingleton<IDelegationChangeEventQueue, DelegationChangeEventQueue>();
    services.AddSingleton<IEventMapperService, EventMapperService>();
    services.AddSingleton<IAccessManagementWrapper, AccessManagementWrapper>();
    services.AddSingleton<IAccessListAuthorization, AccessListAuthorization>();
    services.AddSingleton<IPublicSigningKeyProvider, PublicSigningKeyProvider>();

    services.Configure<GeneralSettings>(config.GetSection("GeneralSettings"));
    services.Configure<AzureStorageConfiguration>(config.GetSection("AzureStorageConfiguration"));
    services.Configure<AzureCosmosSettings>(config.GetSection("AzureCosmosSettings"));
    services.Configure<PostgreSQLSettings>(config.GetSection("PostgreSQLSettings"));
    services.Configure<PlatformSettings>(config.GetSection("PlatformSettings"));
    services.Configure<KeyVaultSettings>(config.GetSection("kvSetting"));
    OedAuthzMaskinportenClientSettings oedAuthzMaskinportenClientSettings = config.GetSection("OedAuthzMaskinportenClientSettings").Get<OedAuthzMaskinportenClientSettings>();
    services.Configure<OedAuthzMaskinportenClientSettings>(config.GetSection("OedAuthzMaskinportenClientSettings"));
    services.AddMaskinportenHttpClient<SettingsJwkClientDefinition, OedAuthzMaskinportenClientDefinition>(oedAuthzMaskinportenClientSettings);
    services.Configure<QueueStorageSettings>(config.GetSection("QueueStorageSettings"));
    services.AddHttpClient<AccessManagementClient>();
    services.AddHttpClient<IRegisterService, RegisterService>();
    services.AddHttpClient<PartyClient>();
    services.AddHttpClient<ProfileClient>();
    services.AddHttpClient<RolesClient>();
    services.AddHttpClient<SBLClient>();
    services.AddHttpClient<ResourceRegistryClient>();
    services.AddHttpClient<OedAuthzClient>();
    services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddTransient<ISigningCredentialsResolver, SigningCredentialsResolver>();
    services.AddSingleton<IEventsQueueClient, EventsQueueClient>();
    services.AddSingleton<IEventLog, EventLogService>();
    services.TryAddSingleton(TimeProvider.System);
    GeneralSettings generalSettings = config.GetSection("GeneralSettings").Get<GeneralSettings>();
    services.AddAuthentication(JwtCookieDefaults.AuthenticationScheme)
        .AddJwtCookie(JwtCookieDefaults.AuthenticationScheme, options =>
        {
            options.JwtCookieName = generalSettings.RuntimeCookieName;
            options.MetadataAddress = generalSettings.OpenIdWellKnownEndpoint;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
        });

    services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthzConstants.POLICY_STUDIO_DESIGNER, policy => policy.Requirements.Add(new ClaimAccessRequirement("urn:altinn:app", "studio.designer")));
        options.AddPolicy(AuthzConstants.ALTINNII_AUTHORIZATION, policy => policy.Requirements.Add(new ClaimAccessRequirement("urn:altinn:app", "sbl.authorization")));
        options.AddPolicy(AuthzConstants.POLICY_PLATFORMISSUER_ACCESSTOKEN, policy => policy.Requirements.Add(new AccessTokenRequirement(AuthzConstants.PLATFORM_ACCESSTOKEN_ISSUER)));
        options.AddPolicy(AuthzConstants.DELEGATIONEVENT_FUNCTION_AUTHORIZATION, policy => policy.Requirements.Add(new ClaimAccessRequirement("urn:altinn:app", "platform.authorization")));
        options.AddPolicy(AuthzConstants.AUTHORIZESCOPEACCESS, policy => policy.Requirements.Add(new ScopeAccessRequirement([AuthzConstants.AUTHORIZE_SCOPE, AuthzConstants.AUTHORIZE_ADMIN_SCOPE])));
    });

    services.AddTransient<IAuthorizationHandler, ClaimAccessHandler>();
    services.AddTransient<IAuthorizationHandler, ScopeAccessHandler>();
    services.AddSingleton<IAuthorizationHandler, AccessTokenHandler>();

    services.AddPlatformAccessTokenSupport(config, builder.Environment.IsDevelopment());

    services.Configure<KestrelServerOptions>(options =>
    {
        options.AllowSynchronousIO = true;
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

    services.AddMvc(options =>
    {
        // Adding custom model binders
        options.ModelBinderProviders.Insert(0, new XacmlRequestApiModelBinderProvider());
        options.RespectBrowserAcceptHeader = true;
    });

    // Add Swagger support (Swashbuckle)
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Altinn Platform Authorization", Version = "v1" });

        try
        {
            string filePath = GetXmlCommentsPathForControllers();
            options.IncludeXmlComments(filePath);
        }
        catch
        {
            // catch swashbuckle exception if it doesn't find the generated xml documentation file
        }

        options.AddSecurityDefinition("AuthorizeAPI", new OpenApiSecurityScheme
        {
            Name = "AuthorizeAPI",
            Description = $"Requires one of the following Scopes: [{AuthzConstants.AUTHORIZE_SCOPE}, {AuthzConstants.AUTHORIZE_ADMIN_SCOPE}]",
            Type = SecuritySchemeType.Http,
            In = ParameterLocation.Header,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        options.AddSecurityDefinition("SubscriptionKey", new OpenApiSecurityScheme
        {
            Name = "SubscriptionKey",
            Description = $"Requires a valid product subscription key as header value: \"Ocp-Apim-Subscription-Key\"",
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header
        });
        options.OperationFilter<SecurityRequirementsOperationFilter>();

        var originalIdSelector = options.SchemaGeneratorOptions.SchemaIdSelector;
        options.SchemaGeneratorOptions.SchemaIdSelector = (Type t) =>
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

    services.AddUrnSwaggerSupport();

    services.AddFeatureManagement();
}

static string GetXmlCommentsPathForControllers()
{
    // locate the xml file being generated by .NET
    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    return xmlPath;
}

void Configure()
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
        app.UseExceptionHandler("/authorization/api/v1/error");
    }

    if (builder.Configuration.GetValue<bool>("PostgreSQLSettings:EnableDBConnection"))
    {
        ConsoleTraceService traceService = new ConsoleTraceService { IsDebugEnabled = true };

        string connectionString = string.Format(
            builder.Configuration.GetValue<string>("PostgreSQLSettings:AdminConnectionString"),
            builder.Configuration.GetValue<string>("PostgreSQLSettings:authorizationDbAdminPwd"));

        app.UseYuniql(
            new PostgreSqlDataService(traceService),
            new PostgreSqlBulkImportService(traceService),
            traceService,
            new Yuniql.AspNetCore.Configuration
            {
                Workspace = Path.Combine(Environment.CurrentDirectory, builder.Configuration.GetValue<string>("PostgreSQLSettings:WorkspacePath")),
                ConnectionString = connectionString,
                IsAutoCreateDatabase = false,
                IsDebug = true
            });
    }

    app.UseSwagger(o => o.RouteTemplate = "authorization/swagger/{documentName}/swagger.json");

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/authorization/swagger/v1/swagger.json", "Altinn Platform Authorization API");
        c.RoutePrefix = "authorization/swagger";
    });

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");
}
