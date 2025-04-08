using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Data;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// API Endpoint extension for DbAccess services
/// </summary>
public static class EndpointExtension
{
    /// <summary>
    /// MapAllDefinitionEndpoints
    /// </summary>
    /// <param name="app">WebApplication</param>
    /// <returns></returns>
    public static WebApplication MapAllDefinitionEndpoints(this WebApplication app)
    {
        Type thisType = typeof(EndpointExtension);
        var definitions = app.Services.GetRequiredService<DbDefinitionRegistry>().GetAllDefinitions();

        foreach (var definition in definitions)
        {
            try
            {
                Type entityType = definition.ModelType;
                Type serviceType = GetRepositoryContract(entityType);
                Type crossRefA = definition.CrossRelation?.AType;
                Type crossRefB = definition.CrossRelation?.BType;
                Type extendedType = definition.Relations.FirstOrDefault()?.ExtendedType ?? definition.CrossRelation?.CrossExtendedType;

                bool isCross = crossRefA != null && crossRefB != null && extendedType != null;
                bool isExtended = extendedType != null;

                if (isExtended)
                {
                    var method = thisType.GetMethod(nameof(MapDefaultsExt))!.MakeGenericMethod(serviceType, entityType, extendedType);
                    method.Invoke(null, new object[] { app, isCross });
                }

                var basemethod = thisType.GetMethod(nameof(MapDefaults))!.MakeGenericMethod(serviceType, entityType);
                basemethod.Invoke(null, new object[] { app, isExtended, isCross });

                if (isCross)
                {
                    var method = thisType.GetMethod(nameof(MapCrossDefaults))!.MakeGenericMethod(serviceType, entityType, extendedType, crossRefA, crossRefB);
                    method.Invoke(null, new object[] { app });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        return app;
    }

    /// <summary>
    /// Generate endpoints for basic types
    /// </summary>
    /// <param name="app">WebApplication</param>
    /// <param name="hasExtendedType">This type has an extended type</param>
    /// <param name="isCross">is cross</param>
    /// <returns></returns>
    public static WebApplication MapDefaults<TService, T>(this WebApplication app, bool hasExtendedType, bool isCross)
        where TService : IDbBasicRepository<T>
    {
        string name = typeof(T).Name;

        bool mapPost = !isCross;
        bool mapPut = !isCross;
        bool mapDelete = !isCross;
        bool mapGet = !hasExtendedType && !isCross;
        bool mapGetAll = !hasExtendedType && !isCross;

        var def = app.Services.GetRequiredService<DbDefinitionRegistry>().GetOrAddDefinition<T>();

        //// bool mapIngest = false;
        //// bool mapSearch = false;

        if (mapGet)
        {
            app.MapGet($"/{name}/{{id}}", async ([FromServices] TService service, Guid id, HttpRequest request) =>
            {
                return await service.Get([new GenericFilter("Id", id)], GenerateRequestOptions(request));
            }).WithOpenApi().WithTags(name).WithSummary($"Get single {name}");
        }

        if (mapGetAll)
        {
            app.MapGet($"/{name}", async ([FromServices] TService service, HttpRequest request) =>
            {
                return await service.Get(GenerateRequestOptions(request));
            }).WithOpenApi().WithTags(name).WithSummary($"Get all {Pluralize(name)}");
        }

        if (mapPost && !def.IsView)
        {
            app.MapPost($"/{name}", async ([FromServices] TService service, [FromBody] T obj, HttpRequest request) =>
            {
                var res = await service.Create(obj, GenerateChangeRequestOptions(request));
                return res > 0 ? Results.Created($"/{name}/{res}", obj) : Results.Problem();
            }).WithOpenApi().WithTags(name).WithSummary($"Create {name}");
        }

        if (mapPut && !def.IsView)
        {
            app.MapPut($"/{name}/{{id}}", async ([FromServices] TService service, Guid id, [FromBody] T obj, HttpRequest request) =>
            {
                return await service.Update(id, obj, GenerateChangeRequestOptions(request));
            }).WithOpenApi().WithTags(name).WithSummary($"Update {name}");
        }

        if (mapDelete && !def.IsView)
        {
            app.MapDelete($"/{name}/{{id}}", async ([FromServices] TService service, Guid id, HttpRequest request) =>
            {
                return await service.Delete(id, GenerateChangeRequestOptions(request));
            }).WithOpenApi().WithTags(name).WithSummary($"Delete {name}");
        }

        return app;
    }

    /// <summary>
    /// Generate endpoints for extended types
    /// </summary>
    /// <param name="app">WebApplication</param>
    /// <param name="isCross">This type has cross definition</param>
    /// <returns></returns>
    public static WebApplication MapDefaultsExt<TService, T, TExtended>(this WebApplication app, bool isCross)
        where TService : IDbExtendedRepository<T, TExtended>
    {
        string name = typeof(T).Name;

        bool mapGet = !isCross;
        bool mapGetAll = !isCross;

        if (mapGet)
        {
            app.MapGet($"/{name}/{{id}}", async ([FromServices] TService service, Guid id, HttpRequest request) =>
            {
                return await service.GetExtended([new GenericFilter("Id", id)], GenerateRequestOptions(request));
            }).WithOpenApi().WithTags(name).WithSummary($"Get single {name}");
        }

        if (mapGetAll)
        {
            app.MapGet($"/{name}", async ([FromServices] TService service, HttpRequest request) =>
            {
                return await service.GetExtended(new List<GenericFilter>(), GenerateRequestOptions(request));
            }).WithOpenApi().WithTags(name).WithSummary($"Get all {Pluralize(name)}");
        }

        return app;
    }

    /// <summary>
    /// Generate endpoints for cross-references
    /// </summary>
    /// <param name="app">WebApplication</param>
    /// <returns></returns>
    public static WebApplication MapCrossDefaults<TService, T, TExtended, TA, TB>(this WebApplication app)
        where TService : IDbCrossRepository<T, TExtended, TA, TB>
    {
        string name = typeof(T).Name;

        string nameA = typeof(TA).Name;
        string nameB = typeof(TB).Name;

        string endpointAGroup = nameA + "Extended";
        string endpointBGroup = nameB + "Extended";

        string pluralNameForA = Pluralize(nameA);
        string pluralNameForB = Pluralize(nameB);

        /*A*/
        app.MapGet($"/{nameA}/{{id}}/{pluralNameForB}", async ([FromServices] TService service, Guid id, HttpRequest request) =>
        {
            return await service.GetA(id, GenerateRequestOptions(request));
        }).WithOpenApi().WithTags(endpointAGroup).WithSummary($"Get {pluralNameForB} based on {name}");

        app.MapPost($"/{nameA}/{{id}}/{nameB}/{{linkId}}", async ([FromServices] TService service, Guid id, Guid linkId) =>
        {
            var res = await service.CreateCross(id, linkId);
            return res > 0 ? Results.Created($"/{name}/{res}", null) : Results.Problem();
        }).WithOpenApi().WithTags(endpointAGroup).WithSummary($"Create {name}");

        app.MapDelete($"/{nameA}/{{id}}/{nameB}/{{linkId}}", async ([FromServices] TService service, Guid id, Guid linkId) =>
        {
            return await service.DeleteCross(id, linkId);
        }).WithOpenApi().WithTags(endpointAGroup).WithSummary($"Delete {name}");

        /*B*/
        app.MapGet($"/{nameB}/{{id}}/{pluralNameForA}", async ([FromServices] TService service, Guid id, HttpRequest request) =>
        {
            return await service.GetB(id, GenerateRequestOptions(request));
        }).WithOpenApi().WithTags(endpointBGroup).WithSummary($"Get {pluralNameForA} based on {name}");

        app.MapPost($"/{nameB}/{{id}}/{nameA}/{{linkId}}", async ([FromServices] TService service, Guid id, Guid linkId) =>
        {
            var res = await service.CreateCross(linkId, id);
            return res > 0 ? Results.Created($"/{name}/{res}", null) : Results.Problem();
        }).WithOpenApi().WithTags(endpointBGroup).WithSummary($"Create {name}");

        app.MapDelete($"/{nameB}/{{id}}/{nameA}/{{linkId}}", async ([FromServices] TService service, Guid id, Guid linkId) =>
        {
            return await service.DeleteCross(linkId, id);
        }).WithOpenApi().WithTags(endpointBGroup).WithSummary($"Delete {name}");

        return app;
    }

    private static ChangeRequestOptions GenerateChangeRequestOptions(HttpRequest request)
    {
        var options = new ChangeRequestOptions()
        {
            ChangedBy = AuditDefaults.EnduserApi, // TODO: Get UserId
            ChangedBySystem = AuditDefaults.EnduserApi
        };
        
        return options;
    }

    private static RequestOptions GenerateRequestOptions(HttpRequest request)
    {
        var options = new RequestOptions();

        if (request.Headers.TryGetValue("x-as-of-time", out var asOfTime))
        {
            options.AsOf = DateTime.Parse(asOfTime.ToString());
        }

        if (request.Query.TryGetValue("page", out var page) && int.TryParse(page, out var pageNumber))
        {
            options.UsePaging = true;
            options.PageNumber = pageNumber;
        }

        if (request.Query.TryGetValue("page-size", out var pageSize) && int.TryParse(pageSize, out var size))
        {
            options.PageSize = size;
        }

        return options;
    }

    private static string Pluralize(string name)
    {
        return name.EndsWith("y") ? name[..^1] + "ies" : name + "s";
    }

    private static Type GetRepositoryContract(Type entityType)
    {
        string serviceName = $"I{entityType.Name}Repository";
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == serviceName) ?? throw new Exception($"Could not find repository interface for '{entityType.Name}'");
    }
}
