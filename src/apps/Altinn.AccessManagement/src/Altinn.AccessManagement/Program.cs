using System.Diagnostics.CodeAnalysis;
using Altinn.AccessManagement;
using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.Core.HostedServices;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.Authorization.Host.Lease;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;

AppDomain domain = AppDomain.CurrentDomain;
domain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(2));

WebApplication app = AccessManagementHost.Create(args);
using var scope = app.Services.CreateScope();
var appsettings = scope.ServiceProvider.GetRequiredService<IOptions<AccessManagementAppsettings>>().Value;

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
app.UseEfAudit();

app.MapControllers();
app.MapDefaultAltinnEndpoints();

await app.RunAsync();

async Task Init()
{
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

    var leaseService = scope.ServiceProvider.GetRequiredService<ILeaseService>();
    await using var lease = await leaseService.AcquireBlocking("access_management_init", cts.Token);
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync(cts.Token);
    await Altinn.AccessMgmt.PersistenceEF.Data.StaticDataIngest.IngestAll(dbContext);

    var registerImport = scope.ServiceProvider.GetRequiredService<RegisterHostedService>();
    await registerImport.EnsureDbIsIngestWithRegisterData(cts.Token);
}

/// <summary>
/// Startup class.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class Program { }
