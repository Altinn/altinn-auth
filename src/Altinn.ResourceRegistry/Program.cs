using Altinn.ResourceRegistry;
using Microsoft.IdentityModel.Logging;

WebApplication app = ResourceRegistryHost.Create(args);

app.AddDefaultAltinnMiddleware(errorHandlingPath: "/resourceregistry/api/v1/error");

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

app.MapDefaultAltinnEndpoints();
app.MapControllers();

app.Run();

/// <summary>
/// Startup class.
/// </summary>
public partial class Program 
{
}
