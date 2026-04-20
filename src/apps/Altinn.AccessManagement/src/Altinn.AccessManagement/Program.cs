using System.Diagnostics.CodeAnalysis;
using Altinn.AccessManagement;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Appsettings;
using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.Core.HostedServices;
using Altinn.AccessMgmt.Persistence.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Logging;

AppDomain domain = AppDomain.CurrentDomain;
domain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(2));

WebApplication app = AccessManagementHost.Create(args);
using var scope = app.Services.CreateScope();
var appsettings = scope.ServiceProvider.GetRequiredService<IOptions<AccessManagementAppsettings>>().Value;
var featureManager = scope.ServiceProvider.GetRequiredService<FeatureManager>();
await app.DefineAccessMgmtDbModels();
await PersistenceFeatures();

if (appsettings.RunInitOnly)
{
    await Init();
    return;
}
else if (appsettings.RunIntegrationTests)
{
    await Init();
}

app.AddDefaultAltinnMiddleware(errorHandlingPath: "/accessmanagement/api/v1/error");

if (app.Environment.IsDevelopment())
{
    // Enable higher level of detail in exceptions related to JWT validation
    IdentityModelEventSource.ShowPII = true;

    // Enable Swagger
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseDbAudit();
app.UseEfAudit();

app.MapControllers();
app.MapDefaultAltinnEndpoints();

await app.RunAsync();

async Task Init()
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await Altinn.AccessMgmt.PersistenceEF.Data.StaticDataIngest.IngestAll(dbContext);

    using var cts = new CancellationTokenSource();
    AppDomain.CurrentDomain.ProcessExit += (s, e) =>
    {
        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Terminated by itself.
        }
    };

    var registerImport = scope.ServiceProvider.GetRequiredService<RegisterHostedService>();
    await registerImport.EnsureDbIsIngestWithRegisterData(cts.Token);
}

async Task PersistenceFeatures()
{
    // Delete me after next prod release
    Altinn.AccessMgmt.PersistenceEF.Utils.Settings.FeatureFlags.UseInstanceDelegationEF = await featureManager.IsEnabledAsync("AccessManagement.InstanceDelegation.EF");

    // Delete me after june 19th and Altinn 2 is no more
    AuthorizedPartiesSettings.IncludeAltinn2 = await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.AuthorizedPartiesIncludeAltinn2);
    AuthorizedPartiesSettings.UsingConnectionQueryOnly = await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.AuthorizedPartiesUsingConnectionQueryOnly);
}

/// <summary>
/// Startup class.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class Program { }
