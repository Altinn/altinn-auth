using System.Diagnostics.CodeAnalysis;
using Altinn.AccessManagement;
using Altinn.AccessMgmt.Persistence.Extensions;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Logging;

AppDomain domain = AppDomain.CurrentDomain;
domain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(2));

WebApplication app = AccessManagementHost.Create(args);

if (await app.Services.GetRequiredService<FeatureManager>().IsEnabledAsync(AccessManagementFeatureFlags.MigrationDb))
{
    await app.UseAccessMgmtDb();
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

app.MapControllers();
app.MapDefaultAltinnEndpoints();

await app.RunAsync();

/// <summary>
/// Startup class.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class Program { }
