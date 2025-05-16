using Altinn.AccessManagement.Api.Enduser;
using Altinn.AccessManagement.Api.Enduser.Authorization.AuthorizationHandler;
using Altinn.AccessManagement.Api.Enduser.Authorization.AuthorizationRequirement;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Extensions;
using Altinn.AccessManagement.Health;
using Altinn.AccessManagement.HostedServices.Contracts;
using Altinn.AccessManagement.HostedServices.Services;
using Altinn.AccessManagement.Integration.Configuration;
using Altinn.AccessManagement.Integration.Extensions;
using Altinn.AccessManagement.Persistence.Configuration;
using Altinn.AccessManagement.Persistence.Extensions;
using Altinn.AccessMgmt.Persistence.Extensions;
using Altinn.Authorization.AccessManagement;
using Altinn.Authorization.Host;
using Altinn.Authorization.Host.Database;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Host.Startup;
using Altinn.Authorization.Integration.Platform.Extensions;
using Altinn.Authorization.ServiceDefaults;
using Altinn.Common.AccessToken;
using Altinn.Common.AccessToken.Configuration;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.Authentication.Configuration;
using Altinn.Common.Authentication.Models;
using Altinn.Common.PEP.Authorization;
using Altinn.Common.PEP.Clients;
using Altinn.Common.PEP.Implementation;
using Altinn.Common.PEP.Interfaces;
using AltinnCore.Authentication.JwtCookie;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry;
using Swashbuckle.AspNetCore.Filters;

namespace Altinn.AccessManagement;

/// <summary>
/// Configures the register host.
/// </summary>
internal static partial class AccessManagementHost
{
    private static ILogger Logger { get; } = StartupLoggerFactory.Create(nameof(AccessManagementHost));

    /// <summary>
    /// Configures the register host.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    public static WebApplication Create(string[] args)
    {
        Log.CreateAltinnHost(Logger);
        var builder = AltinnHost.CreateWebApplicationBuilder("access-management", args);
        builder.ConfigureAppsettings();
        builder.ConfigureLibsHost();
        builder.Services.AddMemoryCache();
        builder.Services.AddAutoMapper(typeof(Program));
        builder.Services.AddRouting(options => options.LowercaseUrls = true);
        builder.Services.AddControllers();
        builder.Services.AddFeatureManagement();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHealthChecks()
            .AddCheck<HealthCheck>("authorization_admin_health_check");

        builder.ConfigureLibsIntegrations();
        builder.ConfigureAppsettings();
        builder.AddAltinnDatabase(opt =>
        {
            var adminConnectionStringFmt = builder.Configuration.GetValue<string>("PostgreSQLSettings:AdminConnectionString");
            var adminConnectionStringPwd = builder.Configuration.GetValue<string>("PostgreSQLSettings:AuthorizationDbAdminPwd");
            var connectionStringFmt = builder.Configuration.GetValue<string>("PostgreSQLSettings:ConnectionString");
            var connectionStringPwd = builder.Configuration.GetValue<string>("PostgreSQLSettings:AuthorizationDbPwd");
            var appsettings = new AccessManagementAppsettings(builder.Configuration);
            if (string.IsNullOrEmpty(connectionStringFmt) || string.IsNullOrEmpty(adminConnectionStringFmt))
            {
                Log.PgsqlMissingConnectionString(Logger);
                opt.Enabled = false;
            }

            opt.AppSource = new(string.Format(connectionStringFmt, connectionStringPwd));
            opt.MigrationSource = new(string.Format(adminConnectionStringFmt, adminConnectionStringPwd));
            opt.Telemetry.EnableMetrics = true;
            opt.Telemetry.EnableTraces = true;
        });

        if (!builder.Environment.IsDevelopment())
        {
            builder.Services.AddOpenTelemetry()
                .UseAzureMonitor();
        }

        builder.ConfigurePostgreSqlConfiguration();
        builder.ConfigureAltinnPackages();
        builder.ConfigureInternals();
        builder.ConfigureOpenAPI();
        builder.ConfigureAuthorization();
        builder.ConfigureAccessManagementPersistence();
        builder.ConfigureHostedServices();
        builder.AddAccessManagementEnduser();

        return builder.Build();
    }

    private static WebApplicationBuilder ConfigureAccessManagementPersistence(this WebApplicationBuilder builder)
    {
        builder.AddAccessMgmtDb(opts =>
        {
            builder.Configuration.GetSection("AccessMgmtPersistenceOptions").Bind(opts);
        });

        return builder;
    }

    private static WebApplicationBuilder ConfigureHostedServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<RegisterHostedService>();
        builder.Services.AddSingleton<IPartySyncService, PartySyncService>();
        builder.Services.AddSingleton<IRoleSyncService, RoleSyncService>();
        builder.Services.AddSingleton<IResourceSyncService, ResourceSyncService>();
        return builder;
    }

    private static WebApplicationBuilder ConfigureLibsIntegrations(this WebApplicationBuilder builder)
    {
        builder.Services.AddAltinnPlatformIntegrationDefaults(() =>
        {
            var appsettings = new AccessManagementAppsettings(builder.Configuration);
            appsettings.Platform.Token.TestTool.Environment = appsettings.Environment;
            if (builder.Configuration.GetValue<Uri>("kvSetting:SecretUri") is var endpoint && endpoint != null)
            {
                appsettings.Platform.Token.KeyVault.Endpoint = endpoint;
            }

            return appsettings.Platform;
        });

        return builder;
    }

    private static WebApplicationBuilder ConfigureLibsHost(this WebApplicationBuilder builder)
    {
        builder.AddAzureAppConfigurationDefaults(opts =>
        {
            var appsettings = new AccessManagementAppsettings(builder.Configuration);
            opts.Endpoint = appsettings.AppConfiguration.Endpoint;
            opts.Enabled = appsettings.AppConfiguration.Enabled;
            opts.AddDefaultLabels(appsettings.Environment, builder.GetAltinnServiceDescriptor().Name);
        });

        builder.AddAltinnLease(opts =>
        {
            var appsettings = new AccessManagementAppsettings(builder.Configuration);
            if (appsettings.Lease?.StorageAccount?.BlobEndpoint == null)
            {
                opts.Type = AltinnLeaseType.InMemory;
            }
            else
            {
                opts.StorageAccount.BlobEndpoint = appsettings.Lease.StorageAccount.BlobEndpoint;
                opts.Type = AltinnLeaseType.AzureStorageAccount;
            }
        });

        return builder;
    }

    private static WebApplicationBuilder ConfigureAltinnPackages(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IPublicSigningKeyProvider, PublicSigningKeyProvider>();
        builder.Services.AddSingleton<IPDP, PDPAppSI>();
        builder.Services.AddHttpClient<AuthorizationApiClient>();
        return builder;
    }

    private static void ConfigureInternals(this WebApplicationBuilder builder)
    {
        builder.AddAccessManagementCore();
        builder.AddAccessManagementIntegration();
        builder.AddAccessManagementPersistence();
    }

    private static void ConfigureOpenAPI(this WebApplicationBuilder builder)
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

        builder.Services.AddUrnSwaggerSupport();
    }

    private static void ConfigureAppsettings(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        builder.Services.Configure<AccessManagementAppsettings>(builder.Configuration.Bind);
        builder.Services.Configure<GeneralSettings>(config.GetSection("GeneralSettings"));
        builder.Services.Configure<PlatformSettings>(config.GetSection("PlatformSettings"));
        builder.Services.Configure<Altinn.Common.PEP.Configuration.PlatformSettings>(config.GetSection("PlatformSettings"));
        builder.Services.Configure<CacheConfig>(config.GetSection("CacheConfig"));
        builder.Services.Configure<PostgreSQLSettings>(config.GetSection("PostgreSQLSettings"));
        builder.Services.Configure<AzureStorageConfiguration>(config.GetSection("AzureStorageConfiguration"));
        builder.Services.Configure<SblBridgeSettings>(config.GetSection("SblBridgeSettings"));
        builder.Services.Configure<KeyVaultSettings>(config.GetSection("kvSetting"));
        builder.Services.Configure<OidcProviderSettings>(config.GetSection("OidcProviders"));
        builder.Services.Configure<UserProfileLookupSettings>(config.GetSection("UserProfileLookupSettings"));
        builder.Services.Configure<AppsInstanceDelegationSettings>(config.GetSection("AppsInstanceDelegationSettings"));
    }

    private static void ConfigureAuthorization(this WebApplicationBuilder builder)
    {
        PlatformSettings platformSettings = builder.Configuration.GetSection("PlatformSettings").Get<PlatformSettings>();
        OidcProviderSettings oidcProviders = builder.Configuration.GetSection("OidcProviders").Get<OidcProviderSettings>();

        if (oidcProviders.TryGetValue("altinn", out OidcProvider altinnOidcProvder))
        {
            builder.Services.AddAuthentication(JwtCookieDefaults.AuthenticationScheme)
            .AddJwtCookie(JwtCookieDefaults.AuthenticationScheme, options =>
            {
                options.JwtCookieName = platformSettings.JwtCookieName;
                options.MetadataAddress = altinnOidcProvder.Issuer;
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
        }

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(AuthzConstants.PLATFORM_ACCESS_AUTHORIZATION, policy => policy.Requirements.Add(new AccessTokenRequirement()))
            .AddPolicy(AuthzConstants.ALTINNII_AUTHORIZATION, policy => policy.Requirements.Add(new ClaimAccessRequirement("urn:altinn:app", "sbl.authorization")))
            .AddPolicy(AuthzConstants.INTERNAL_AUTHORIZATION, policy => policy.Requirements.Add(new ClaimAccessRequirement("urn:altinn:app", "internal.authorization")))
            .AddPolicy(AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_READ, policy => policy.Requirements.Add(new ResourceAccessRequirement("read", "altinn_maskinporten_scope_delegation")))
            .AddPolicy(AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_WRITE, policy => policy.Requirements.Add(new ResourceAccessRequirement("write", "altinn_maskinporten_scope_delegation")))
            .AddPolicy(AuthzConstants.POLICY_MASKINPORTEN_DELEGATIONS_PROXY, policy => policy.Requirements.Add(new ScopeAccessRequirement(["altinn:maskinporten/delegations", "altinn:maskinporten/delegations.admin"])))
            .AddPolicy(AuthzConstants.POLICY_ACCESS_MANAGEMENT_READ, policy => policy.Requirements.Add(new ResourceAccessRequirement("read", "altinn_access_management")))
            .AddPolicy(AuthzConstants.POLICY_ACCESS_MANAGEMENT_WRITE, policy => policy.Requirements.Add(new ResourceAccessRequirement("write", "altinn_access_management")))
            .AddPolicy(AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ, policy => policy.Requirements.Add(new EndUserResourceAccessRequirement("read", "altinn_enduser_access_management", false)))
            .AddPolicy(AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE, policy => policy.Requirements.Add(new EndUserResourceAccessRequirement("read", "altinn_enduser_access_management", false)))
            .AddPolicy(AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ_WITH_PASS_TROUGH, policy => policy.Requirements.Add(new EndUserResourceAccessRequirement("read", "altinn_enduser_access_management", true)))
            .AddPolicy(AuthzConstants.POLICY_RESOURCEOWNER_AUTHORIZEDPARTIES, policy => policy.Requirements.Add(new ScopeAccessRequirement([AuthzConstants.SCOPE_AUTHORIZEDPARTIES_RESOURCEOWNER, AuthzConstants.SCOPE_AUTHORIZEDPARTIES_ADMIN])))
            .AddPolicy(AuthzConstants.POLICY_CLIENTDELEGATION_READ, policy => policy.Requirements.Add(new EndUserResourceAccessRequirement("read", "altinn_client_administration")))
            .AddPolicy(AuthzConstants.POLICY_CLIENTDELEGATION_WRITE, policy => policy.Requirements.Add(new EndUserResourceAccessRequirement("write", "altinn_client_administration")))
            .AddPolicy(AuthzConstants.SCOPE_PORTAL_ENDUSER, policy => policy.Requirements.Add(new ScopeAccessRequirement([AuthzConstants.SCOPE_PORTAL_ENDUSER])));

        builder.Services.AddScoped<IAuthorizationHandler, AccessTokenHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ClaimAccessHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ResourceAccessHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, EndUserResourceAccessHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, ScopeAccessHandler>();
    }

    private static void ConfigurePostgreSqlConfiguration(this WebApplicationBuilder builder)
    {
        var runMigrations = builder.Configuration.GetValue<bool>("PostgreSQLSettings:EnableDBConnection");
        var adminConnectionStringFmt = builder.Configuration.GetValue<string>("PostgreSQLSettings:AdminConnectionString");
        var adminConnectionStringPwd = builder.Configuration.GetValue<string>("PostgreSQLSettings:AuthorizationDbAdminPwd");
        var connectionStringFmt = builder.Configuration.GetValue<string>("PostgreSQLSettings:ConnectionString");
        var connectionStringPwd = builder.Configuration.GetValue<string>("PostgreSQLSettings:AuthorizationDbPwd");

        var adminConnectionString = new NpgsqlConnectionStringBuilder(string.Format(adminConnectionStringFmt, adminConnectionStringPwd));
        var connectionString = new NpgsqlConnectionStringBuilder(string.Format(connectionStringFmt, connectionStringPwd))
        {
            MaxAutoPrepare = 50,
            AutoPrepareMinUsages = 2,
            IncludeErrorDetail = true
        };

        var serviceDescriptor = builder.Services.GetAltinnServiceDescriptor();
        var existing = builder.Configuration.GetValue<string>($"ConnectionStrings:{serviceDescriptor.Name}_db");

        if (!string.IsNullOrEmpty(existing))
        {
            return;
        }

        builder.Configuration.AddInMemoryCollection([
            KeyValuePair.Create($"ConnectionStrings:{serviceDescriptor.Name}_db", connectionString.ToString()),
            KeyValuePair.Create($"ConnectionStrings:{serviceDescriptor.Name}_db_migrate", adminConnectionString.ToString()),
            KeyValuePair.Create($"Altinn:Npgsql:{serviceDescriptor.Name}:Migrate:Enabled", runMigrations ? "true" : "false"),
        ]);
    }

    static partial class Log
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "Creating Altinn host.")]
        internal static partial void CreateAltinnHost(ILogger logger);

        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Configuration setting '{field}' is null or empty.")]
        internal static partial void ConfigValueIsNullOrEmpty(ILogger logger, string field);

        [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Connection string(s) for pgsql server are missing")]
        internal static partial void PgsqlMissingConnectionString(ILogger logger);
    }
}
