using System.Diagnostics.CodeAnalysis;
using Altinn.AccessManagement;
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
await Init();

if (appsettings.RunInitOnly)
{
    return;
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

app.MapControllers();
app.MapDefaultAltinnEndpoints();

await app.RunAsync();

async Task Init()
{
    if (await featureManager.IsEnabledAsync(AccessManagementFeatureFlags.MigrationDbEf))
    {
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }
    else if (await featureManager.IsEnabledAsync(AccessManagementFeatureFlags.MigrationDb))
    {
        bool generateBasicData = await featureManager.IsEnabledAsync(AccessManagementFeatureFlags.MigrationDbWithBasicData);
        await app.UseAccessMgmtDb(generateBasicData);
    }
}

/// <summary>
/// Startup class.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class Program { }
