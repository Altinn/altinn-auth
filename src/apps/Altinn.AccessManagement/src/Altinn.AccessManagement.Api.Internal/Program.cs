using Altinn.AccessManagement.Api.Internal;

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

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

/// <summary>
/// Startup class.
/// </summary>
internal sealed partial class Program
{
    private Program()
    {
    }
}
