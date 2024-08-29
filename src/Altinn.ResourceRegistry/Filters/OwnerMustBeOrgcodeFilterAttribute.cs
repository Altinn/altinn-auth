#nullable enable

using System.Diagnostics;
using Altinn.Authorization.ProblemDetails;
using Altinn.ResourceRegistry.Auth;
using Altinn.ResourceRegistry.Core.Errors;
using Altinn.ResourceRegistry.Core.ServiceOwners;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Altinn.ResourceRegistry.Filters;

/// <summary>
/// A filter that checks that a resource owner is an organization code (and normalizes the value).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
internal sealed class OwnerMustBeOrgcodeFilterAttribute
    : Attribute
    , IFilterFactory
{
    private static readonly ObjectFactory<OwnerMustBeOrgcodeFilter> _factory
        = ActivatorUtilities.CreateFactory<OwnerMustBeOrgcodeFilter>([]);

    /// <inheritdoc/>
    public bool IsReusable => true;

    /// <inheritdoc/>
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        => _factory(serviceProvider, []);

    private sealed class OwnerMustBeOrgcodeFilter
        : IAsyncResourceFilter
    {
        private readonly IServiceOwnerService _service;

        public OwnerMustBeOrgcodeFilter(IServiceOwnerService service)
        {
            _service = service;
        }

        public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var providers = httpContext.GetEndpoint()?.Metadata.GetOrderedMetadata<IResourceOwnerProvider<HttpContext>>();
            if (providers is null)
            {
                return next();
            }

            string? resourceOwner = null;
            IResourceOwnerProvider<HttpContext>? provider = null;
            foreach (var p in providers)
            {
                if (p.TryGetResourceOwner(httpContext, out resourceOwner))
                {
                    provider = p;
                    break;
                }
            }

            if (resourceOwner is null)
            {
                return next();
            }

            var serviceOwnersTask = _service.GetServiceOwners(context.HttpContext.RequestAborted);
            if (!serviceOwnersTask.IsCompletedSuccessfully)
            {
                return WaitAndCheckOwner(context, next, serviceOwnersTask, resourceOwner, provider!);
            }

            #pragma warning disable VSTHRD103 // Call async methods when in an async method
            return CheckOwner(context, next, serviceOwnersTask.Result, resourceOwner, provider!);
            #pragma warning restore VSTHRD103 // Call async methods when in an async method

            static async Task WaitAndCheckOwner(
                ResourceExecutingContext context,
                ResourceExecutionDelegate next,
                ValueTask<ServiceOwnerLookup> serviceOwnersTask,
                string resourceOwner,
                IResourceOwnerProvider<HttpContext> provider)
            {
                var serviceOwners = await serviceOwnersTask;
                await CheckOwner(context, next, serviceOwners, resourceOwner, provider);
            }

            static Task CheckOwner(
                ResourceExecutingContext context,
                ResourceExecutionDelegate next,
                ServiceOwnerLookup serviceOwners,
                string resourceOwner,
                IResourceOwnerProvider<HttpContext> provider)
            {
                if (!serviceOwners.TryGet(resourceOwner, out var serviceOwner))
                {
                    ValidationErrorBuilder builder = default;
                    builder.Add(ValidationErrors.AccessList_Owner_MustBe_OrgCode, provider.Path);

                    var success = builder.TryToActionResult(out var result);
                    Debug.Assert(success);

                    context.Result = result;
                    return Task.CompletedTask;
                }
                else if (!string.Equals(resourceOwner, serviceOwner.OrgCode, StringComparison.Ordinal))
                {
                    provider.SetResourceOwner(context.HttpContext, serviceOwner.OrgCode);
                }

                return next();
            }
        }
    }
}
