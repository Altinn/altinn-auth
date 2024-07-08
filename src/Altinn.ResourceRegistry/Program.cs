using Altinn.ResourceRegistry;
using Microsoft.IdentityModel.Logging;

WebApplication app = ResourceRegistryHost.Create(args);

app.MapDefaultAltinnEndpoints();

Console.WriteLine("Startup // Configure");

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    Console.WriteLine("IsDevelopment || IsStaging");

    app.UseDeveloperExceptionPage();

    // Enable higher level of detail in exceptions related to JWT validation
    IdentityModelEventSource.ShowPII = true;
}
else
{
    app.UseExceptionHandler("/resourceregistry/api/v1/error");
}

app.UseMiddleware<RequestForwarderLogMiddleware>("before forwarder middleware");
app.UseForwardedHeaders();
app.UseMiddleware<RequestForwarderLogMiddleware>("after forwarder middleware");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Startup class.
/// </summary>
public partial class Program 
{
}
