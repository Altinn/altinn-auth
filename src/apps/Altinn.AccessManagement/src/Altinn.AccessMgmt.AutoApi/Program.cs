using Altinn.AccessMgmt.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddAccessMgmtDb(opts =>
{
    builder.Configuration.GetSection("AccessMgmtPersistenceOptions").Bind(opts);
});

var app = builder.Build();

await app.UseAccessMgmtDb();

app.MapAllDefinitionEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
