using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Altinn.AccessManagement.Api.Metadata
{
    /// <summary>
    /// Configures dependencies for Metadata API.
    /// </summary>
    public static partial class AccessManagementMetadataHost
    {
        /// <summary>
        /// Configures OpenAPI (Swagger) services for the specified web application builder.
        /// </summary>    
        /// <param name="builder">The web application builder to configure with OpenAPI and Swagger services. Cannot be null.</param>
        public static void ConfigureOpenAPI(this WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.OperationFilter<SecurityRequirementsOperationFilter>();
                options.EnableAnnotations();

                var originalIdSelector = options.SchemaGeneratorOptions.SchemaIdSelector;
                options.SchemaGeneratorOptions.SchemaIdSelector = (Type t) =>
                {
                    if (!t.IsNested)
                    {
                        var orig = originalIdSelector(t);

                        return orig;
                    }

                    var chain = new List<string>();
                    do
                    {
                        chain.Add(originalIdSelector(t));
                        t = t.DeclaringType;
                    }
                    while (t != null);
                    chain.Reverse();
                    return string.Join(".", chain);
                };
            });

            builder.Services.AddUrnSwaggerSupport();
        }
    }
}
