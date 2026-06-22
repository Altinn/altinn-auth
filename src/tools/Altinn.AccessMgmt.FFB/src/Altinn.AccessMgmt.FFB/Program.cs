using Altinn.AccessMgmt.FFB.Components;
using Altinn.AccessMgmt.FFB.Config;
using Altinn.AccessMgmt.FFB.Jobs;
using Altinn.AccessMgmt.FFB.Jobs.Models;
using Altinn.AccessMgmt.FFB.Services;
using Altinn.AccessMgmt.FFB.Services.Contracts;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory // external appsettings.json is found next to the exe in single-file publish
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.Configure<EnvironmentsConfig>(builder.Configuration);
builder.Services.AddSingleton<IEnvironmentDbContextFactory, EnvironmentDbContextFactory>();
builder.Services.AddScoped<EnvironmentState>();

builder.Services.AddSingleton<IJobRunStore, JobRunStore>();

builder.Services.Configure<NotificationsConfig>(builder.Configuration.GetSection("Notifications"));
builder.Services.AddHttpClient("telegram");
builder.Services.AddSingleton<INotificationService, TelegramNotificationService>();

builder.Services.AddSingleton<IJobRunner, JobRunner>();

builder.Services.Configure<JobSchedulesConfig>(builder.Configuration.GetSection("JobSchedules"));

// Register as the concrete type first so both IJobScheduler and IHostedService share the same instance.
builder.Services.AddSingleton<JobSchedulerService>();
builder.Services.AddSingleton<IJobScheduler>(p => p.GetRequiredService<JobSchedulerService>());
builder.Services.AddHostedService(p => p.GetRequiredService<JobSchedulerService>());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
