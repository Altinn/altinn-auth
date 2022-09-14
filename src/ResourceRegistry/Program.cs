using Altinn.Platform.Authorization.Configuration;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Integration.Clients;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Persistence;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Npgsql.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yuniql.AspNetCore;
using Yuniql.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
ConfigureServices(builder.Services, builder.Configuration);


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

ConfigurePostgreSql();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();



void ConfigureServices(IServiceCollection services, IConfiguration config)
{
    services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

    });

    services.AddSingleton(config);

    services.AddSingleton<IResourceRegistry, ResourceRegistryService>();
    services.AddSingleton<IResourceRegistryRepository, ResourceRepository>();
    services.AddSingleton<IPRP, PRPClient>();
    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.Configure<PostgreSQLSettings>(config.GetSection("PostgreSQLSettings"));

}

void ConfigurePostgreSql()
{
    if (builder.Configuration.GetValue<bool>("PostgreSQLSettings:EnableDBConnection"))
    {
        NpgsqlLogManager.Provider = new ConsoleLoggingProvider(NpgsqlLogLevel.Trace, true, true);

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
}