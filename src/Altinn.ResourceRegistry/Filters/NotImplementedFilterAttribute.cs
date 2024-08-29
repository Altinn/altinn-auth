#nullable enable

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Altinn.ResourceRegistry.Filters;

/// <summary>
/// A filter that converts <see cref="NotImplementedException"/> to a 501 Not Implemented response.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
internal sealed class NotImplementedFilterAttribute 
    : Attribute
    , IExceptionFilter
{
    /// <inheritdoc/>
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is NotImplementedException)
        {
            context.Result = new StatusCodeResult((int)HttpStatusCode.NotImplemented);
        }
    }
}
