using Altinn.AccessManagement.Api.Metadata;
using Altinn.AccessMgmt.Core.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.ConfigureOpenAPI();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable Swagger
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add translation middleware to extract language preferences from headers
app.UseTranslation();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
