namespace Altinn.Register;

/// <summary>
/// Boilerplate Program 
/// </summary>
public static class Program
{
    /// <summary>
    /// Boilerplate Main
    /// </summary>
    /// <param name="args">Boilerplate</param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.Run();
    }
}
