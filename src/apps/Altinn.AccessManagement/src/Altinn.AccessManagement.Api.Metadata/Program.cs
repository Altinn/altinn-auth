using Altinn.AccessManagement.Api.Metadata.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Add translation middleware to extract language preferences from headers
app.UseTranslation();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
