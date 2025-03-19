using System.Reflection;
using Altinn.AccessManagement;
using Altinn.AccessMgmt.FFB.Components;
using Altinn.AccessMgmt.Persistence.Extensions;
using Altinn.Authorization.Host.Database;
using Blazored.LocalStorage;

var builder = WebApplication.CreateBuilder(args);

var assembly = Assembly.Load(new AssemblyName("Altinn.AccessMgmt.FFB"));
builder.Configuration.AddUserSecrets(assembly);

builder.AddAltinnDatabase(opt =>
{
    var appsettings = new AccessManagementAppsettings(builder.Configuration);
    if (string.IsNullOrEmpty(appsettings.Database.Postgres.AppConnectionString) || string.IsNullOrEmpty(appsettings.Database.Postgres.MigrationConnectionString))
    {
        opt.Enabled = false;
    }

    opt.AppSource = new(appsettings.Database.Postgres.AppConnectionString);
    opt.MigrationSource = new(appsettings.Database.Postgres.MigrationConnectionString);
    opt.Telemetry.EnableMetrics = true;
    opt.Telemetry.EnableTraces = true;
});

builder.AddAccessMgmtDb(opts =>
{
    builder.Configuration.GetSection("AccessMgmtPersistenceOptions").Bind(opts);
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var app = builder.Build();

await app.UseAccessMgmtDb();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
