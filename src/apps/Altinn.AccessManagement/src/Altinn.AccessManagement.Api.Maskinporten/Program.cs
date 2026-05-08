var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

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
