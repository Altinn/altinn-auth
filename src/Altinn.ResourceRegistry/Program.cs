using Altinn.ResourceRegistry;
using Microsoft.IdentityModel.Logging;

WebApplication app = ResourceRegistryHost.Create(args);

app.UseMiddleware<RequestForwarderLogMiddleware>("before forwarder middleware");
app.AddDefaultAltinnMiddleware(errorHandlingPath: "/resourceregistry/api/v1/error");
app.UseMiddleware<RequestForwarderLogMiddleware>("after forwarder middleware");

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
